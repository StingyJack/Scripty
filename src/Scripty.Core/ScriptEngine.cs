namespace Scripty.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Compilation;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Scripting;
    using Output;
    using ProjectTree;
    using Resolvers;

    public class ScriptEngine
    {
        #region "fields and props"

        private readonly string _projectFilePath;

        public ProjectRoot ProjectRoot { get; }

        #endregion //#region "fields and props"

        #region  "ctors"

        public ScriptEngine(string projectFilePath)
            : this(projectFilePath, null, null)
        {
        }

        public ScriptEngine(string projectFilePath, string solutionFilePath, IReadOnlyDictionary<string, string> properties)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(projectFilePath));
            }
            if (!Path.IsPathRooted(projectFilePath))
            {
                throw new ArgumentException("Project path must be absolute", nameof(projectFilePath));
            }

            // The solution path is optional. If it's provided, the solution will be loaded and 
            // the project found in the solution. If not, then the project is loaded directly.
            if (solutionFilePath != null)
            {
                if (!Path.IsPathRooted(solutionFilePath))
                {
                    throw new ArgumentException("Solution path must be absolute", nameof(solutionFilePath));
                }
            }

            _projectFilePath = projectFilePath;
            ProjectRoot = new ProjectRoot(projectFilePath, solutionFilePath, properties);
        }

        #endregion //#region  "ctors"

        #region  "script execution"

        public async Task<ScriptResult> Evaluate(ScriptSource source)
        {
            var assembliesToRef = BuildReferenceCollection();
            var namepspaces = BuildNamepspaces();
            var options = BuildScriptOptions(source, assembliesToRef.AsAssemblies(), namepspaces);

            ScriptResult scriptResult;

            using (var context = GetContext(source.FilePath))
            {
                try
                {
                    await CSharpScript.EvaluateAsync(source.Code, options, context);
                    scriptResult = new ScriptResult(context.Output.OutputFiles);
                    await WriteAllOutput(context);
                }
                catch (Exception ex)
                {
                    scriptResult = HandleScriptExecutionException(ex, context);
                }
                return scriptResult;
            }
        }



        public async Task<ScriptState<ScriptContext>> Run(ScriptSource source, ScriptOptions options)
        {
            try
            {
                using (var context = GetContext(source.FilePath))
                {
                    return await CSharpScript.RunAsync<ScriptContext>(source.Code, options, context);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion //#region  "script execution"

        #region  "engine helpers"

        protected static List<string> BuildNamepspaces(IEnumerable<string> additionalNamespaces = null )
        {
            var namepspaces = new List<string>
            {
                "System",
                "Scripty.Core",
                "Scripty.Core.Output",
                "Scripty.Core.ProjectTree"
            };
            if (additionalNamespaces != null)
            {
                namepspaces.AddRange(additionalNamespaces);
            }
            return namepspaces;
        }

        /// <summary>
        ///     Builds the usual assembly reference collection, with references to mscorlib, CodeAnalysis, Scripty.Core, MSBuild, 
        /// anything Scripty.Core is referencing, anything the currently executing assembly is referencing, anything the ambient
        /// project is referencing, and whatever is in the additional assemblies parameter.
        /// </summary>
        /// <param name="additionalAssemblies">Any additional assemblies</param>
        /// <returns></returns>
        protected ReferenceCollection BuildReferenceCollection(IEnumerable<Assembly> additionalAssemblies = null)
        {
            var swTotal = Stopwatch.StartNew();
            var swStep = Stopwatch.StartNew();
            var assembliesToRef = new ReferenceCollection();
            swStep.Stop();
            Wt($"ReferenceCollection instance creation took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            assembliesToRef.Add(typeof(object), typeof(Project), typeof(Microsoft.Build.Evaluation.Project), typeof(ScriptEngine));
            swStep.Stop();
            Wt($"ReferenceCollection add by type took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            assembliesToRef.Add(CompilationHelpers.GetReferencedAssemblies(typeof(ScriptEngine).Assembly));
            swStep.Stop();
            Wt($"ReferenceCollection add script engine references creation took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            assembliesToRef.Add(CompilationHelpers.GetReferencedAssemblies(Assembly.GetExecutingAssembly()));
            swStep.Stop();
            Wt($"ReferenceCollection add executing assembly references creation took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            assembliesToRef.Add(ProjectRoot.Analysis.MetadataReferences);
            swStep.Stop();
            Wt($"ReferenceCollection add ambient project metadata creation took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            assembliesToRef.Add(additionalAssemblies);
            swStep.Stop();
            Wt($"ReferenceCollection add additional assemblies instance creation took {swStep.ElapsedMilliseconds}ms");
            
            swTotal.Stop();
            Wt($"ReferenceCollection total operation time took {swTotal.ElapsedMilliseconds}ms");
            return assembliesToRef;
        }


        protected static ScriptOptions BuildScriptOptions(ScriptSource source, List<Assembly> assembliesToRef, List<string> namepspaces)
        {
            var resolver = new InterceptDirectiveResolver();
            var options = ScriptOptions.Default
                .WithFilePath(source.FilePath)
                .WithReferences(assembliesToRef)
                .WithImports(namepspaces)
                .WithSourceResolver(resolver);
            return options;
        }

        protected ScriptContext GetContext(string scriptFilePath)
        {
            if (scriptFilePath == null)
            {
                throw new ArgumentNullException(nameof(scriptFilePath));
            }

            return new ScriptContext(scriptFilePath, _projectFilePath, ProjectRoot);
        }

        #endregion //#region  "engine helpers"

        #region "output"

        protected async Task WriteAllOutput(ScriptContext context)
        {
            foreach (var of in context.Output.OutputFiles)
            {
                var outputFile = of as OutputFileWriter;
                if (outputFile == null)
                {
                    continue;
                }
                if (outputFile.IsClosed == false)
                {
                    outputFile.Close();
                }

                if (outputFile.FormatterEnabled)
                {
                    await ApplyFormatting(outputFile);
                }
            }
        }

        internal async Task ApplyFormatting(OutputFileWriter outputFile)
        {
            var document = ProjectRoot.Analysis.AddDocument(outputFile.FilePath, File.ReadAllText(outputFile.FilePath));

            var resultDocument = await Formatter.FormatAsync(document,
                outputFile.FormatterOptions.Apply(ProjectRoot.Workspace.Options));
            var resultContent = await resultDocument.GetTextAsync();

            File.WriteAllText(outputFile.FilePath, resultContent.ToString());
        }

        #endregion //#region "output"

        #region  "exception handling"

        protected ScriptResult HandleScriptExecutionException(Exception ex, ScriptContext context)
        {
            var scriptResult = new ScriptResult(context.Output.OutputFiles);
            scriptResult.AddErrors(GetExceptionDiagnosticsValue(ex));
            return scriptResult;
        }

        protected static List<ScriptError> GetExceptionDiagnosticsValue(Exception ex)
        {
            var errors = new List<ScriptError>();
            if (ex is CompilationErrorException)
            {
                var compilationError = ex as CompilationErrorException;
                return BuildScriptErrors(compilationError.Diagnostics);
            }

            if (ex is AggregateException)
            {
                var aggregateException = ex as AggregateException;

                errors.AddRange(aggregateException.InnerExceptions
                    .Select(x => new ScriptError
                    {
                        Message = x.ToString()
                    }).ToList());
            }
            else
            {
                errors.AddRange(new[]
                {
                    new ScriptError
                    {
                        Message = ex.ToString()
                    }
                });
            }
            return errors;
        }

        protected static List<ScriptError> BuildScriptErrors(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.Select(x => new ScriptError
            {
                Message = x.GetMessage(),
                Line = x.Location.GetLineSpan().StartLinePosition.Line,
                Column = x.Location.GetLineSpan().StartLinePosition.Character
            }).ToList();
        }

        /// <summary>
        ///     Writes the specified message to trace
        /// </summary>
        /// <param name="message">The message.</param>
        protected void Wt(string message)
        {
            Trace.WriteLine(message);
        }

        #endregion //#region  "exception handling"
    }
}