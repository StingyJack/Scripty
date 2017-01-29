using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Scripting;
using Scripty.Core.Output;
using Scripty.Core.ProjectTree;

namespace Scripty.Core
{
    public class ScriptEngine
    {
        private readonly string _projectFilePath;

        public OutputBehavior OutputBehavior { get; set; }

        public ScriptEngine(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(projectFilePath));
            }
            if (!Path.IsPathRooted(projectFilePath))
            {
                throw new ArgumentException("Project path must be absolute", nameof(projectFilePath));
            }

            _projectFilePath = projectFilePath;
            ProjectRoot = new ProjectRoot(projectFilePath);
        }

        public ProjectRoot ProjectRoot { get; }

        public async Task<ScriptResult> Evaluate(ScriptSource source)
        {
            ScriptOptions options = ScriptOptions.Default
                .WithFilePath(source.FilePath)
                .WithReferences(
                    typeof(Microsoft.CodeAnalysis.Project).Assembly,  // Microsoft.CodeAnalysis.Workspaces
                    typeof(Microsoft.Build.Evaluation.Project).Assembly,  // Microsoft.Build
                    typeof(ScriptEngine).Assembly  // Scripty.Core
                    )
                .WithImports(
                    "System",
                    "Scripty.Core",
                    "Scripty.Core.Output",
                    "Scripty.Core.ProjectTree");

            ScriptResult scriptResult;
            Exception caughtException = null;
            

            using (ScriptContext context = GetContext(source.FilePath))
            {
                bool writeAllOutputFiles = true;

                try
                {
                    //await CSharpScript.EvaluateAsync(source.Code, options, context);
                    var globals = await CSharpScript.RunAsync(source.Code, options, context, typeof(ScriptContext));
                    
                    scriptResult = new ScriptResult(context.Output.OutputFileInfos);
                }
                catch (CompilationErrorException compilationError)
                {
                    caughtException = compilationError;

                    scriptResult = new ScriptResult(context.Output.OutputFileInfos,
                        compilationError.Diagnostics
                            .Select(x => new ScriptError
                            {
                                Message = x.GetMessage(),
                                Line = x.Location.GetLineSpan().StartLinePosition.Line,
                                Column = x.Location.GetLineSpan().StartLinePosition.Character,
                                FilePath =x.Location.GetLineSpan().Path
                            })
                            .ToList());
                }
                catch (AggregateException aggregateException)
                {
                    caughtException = aggregateException;

                    scriptResult = new ScriptResult(context.Output.OutputFileInfos,
                        aggregateException.InnerExceptions
                            .Select(x => new ScriptError
                            {
                                Message = x.ToString()
                            }).ToList());
                }
                catch (Exception ex)
                {
                    caughtException = ex; 

                    scriptResult = new ScriptResult(context.Output.OutputFileInfos,
                        new[]
                        {
                            new ScriptError
                            {
                                Message = ex.ToString()
                            }
                        });
                }

                switch (OutputBehavior) {

                    case OutputBehavior.DontOverwriteIfEvaluationFails:
                        if (caughtException != null)
                        {
                            //future - if compilation error, do something, else do something else
                            writeAllOutputFiles = false;
                        }
                        break;

                    case OutputBehavior.ScriptControlsOutput:
                        writeAllOutputFiles = true; // this will be examined in the WriteAllOutputFiles
                        break;

                    case OutputBehavior.NeverGenerateOutput:
                        writeAllOutputFiles = false;
                        break;
                }

                if (writeAllOutputFiles)
                {
                    await WriteAllOutputFiles(context);
                }
                CleanupTempFiles(context);
            }
            return scriptResult;
        }

        protected async Task WriteAllOutputFiles(ScriptContext context)
        {
            foreach (var outputFile in context.Output.OutputTempFiles.Values)
            {
                outputFile.Flush();
                outputFile.Close();

                if (outputFile.KeepOutput == false)
                {
                    //TODO: Figure out why there is a discrepancy here.
                    //Per the script, this is be getting set correctly...
                    //   context.Output.KeepOutput == false
                    //But this never seems to change...
                    //  context.Output.OutputTempFiles.First().Value.KeepOutput == true

                    continue;
                }
                
                if (File.Exists(outputFile.TargetFilePath))
                {
                    File.Delete(outputFile.TargetFilePath);
                }

                File.Move(outputFile.TempFilePath, outputFile.TargetFilePath);

                if (outputFile.FormatterEnabled)
                {
                    var document = ProjectRoot.Analysis.AddDocument(outputFile.TargetFilePath, File.ReadAllText(outputFile.TargetFilePath));

                    var resultDocument = await Formatter.FormatAsync(document,
                        outputFile.FormatterOptions.Apply(ProjectRoot.Workspace.Options)
                    );
                    var resultContent = await resultDocument.GetTextAsync();

                    File.WriteAllText(outputFile.TargetFilePath, resultContent.ToString());
                }
            }
        }

        protected void CleanupTempFiles(ScriptContext context)
        {
            foreach (var outputFile in context.Output.OutputTempFiles.Values)
            {
                if (File.Exists(outputFile.TempFilePath))
                {
                    try
                    {
                        File.Delete(outputFile.TempFilePath);
                    }
                    catch (Exception )
                    {
                        //swallow
                    }
                }
            }
        }

        private ScriptContext GetContext(string scriptFilePath)
        {
            if (scriptFilePath == null)
            {
                throw new ArgumentNullException(nameof(scriptFilePath));
            }

            return new ScriptContext(scriptFilePath, _projectFilePath, ProjectRoot);
        }
    }
}
