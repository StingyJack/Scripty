// ReSharper disable CheckNamespace
namespace Scripty.CustomTool
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Core.Compilation;
    using Core;
    using Core.Resolvers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    public class DebugEngine : ScriptEngine
    {
        public DebugEngine(string projectFilePath) : base(projectFilePath)
        {
        }

        public DebugEngine(string projectFilePath, string solutionFilePath, IReadOnlyDictionary<string, string> properties)
            : base(projectFilePath, solutionFilePath, properties)
        {
        }

        public ScriptResult DebugScript(ScriptSource source, IEnumerable<Assembly> additionalAssemblies = null,
            CompileDirection? compileDirection = null)
        {
            if (compileDirection.HasValue == false) { compileDirection = CompileDirection.EverythingBuiltAsClassesAndReffed; }
            var asms = new List<Assembly>();
            if (additionalAssemblies != null)
            {
                asms.AddRange(additionalAssemblies);
            }
            asms.Add(typeof(DebugEngine).Assembly);

            //get the defaults and usual stuffs
            var metadataReferences = BuildAssembliesToRef(asms).ToMetadataReferences().ToList();
            var namespaces = BuildNamepspaces();
            var compilationSources = new List<SyntaxTree>();
            var scriptLoadsAsAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            var scriptFolder = FileUtilities.GetDirectory(source.FilePath);

            //now pick apart the script, loading the metadata refs first because they will be unchanged.
            var executingScriptParseOptions = CsRewriter.DefaultParseOptions; // CompilationHelpers.GetParseOptions(compileDirection.Value);
            var executingScriptCompilationSource = CompilationHelpers.GetRootMainCompilationUnit(source.Code, executingScriptParseOptions);


            //assembliesToRef.AddRange(CompilationHelpers.GetReferencedAssemblies(executingScriptCompilationSource, scriptFolder));
            var refExtract = CompilationHelpers.ExtractReferenceDirectives(executingScriptCompilationSource, scriptFolder);
            executingScriptCompilationSource = refExtract.Item1;
            metadataReferences.AddRange(refExtract.Item2.Select(r => MetadataReference.CreateFromFile(r.Location)));


            //this is the harder part. Each #load directive will need its content either compiled as 
            // a standard assembly with pdb, or as script assembly with pdb. Debugging requires pdb's.
            var loadDirectives = executingScriptCompilationSource.GetLoadDirectives();
            foreach (var loadDirective in loadDirectives)
            {
                var path = FileUtilities.BuildFullPath(scriptFolder, loadDirective.File.ValueText);
                var asmDetail = CsRewriter.GetRewriteAssemblyPaths(path);
                var referencedCompilation = CompilationHelpers.GetCompilationForLoadDirective(compileDirection.Value, path, asmDetail, namespaces,
                    metadataReferences);
                var scriptErrors = BuildScriptErrors(referencedCompilation.CompilationResult.Diagnostics);

                Debug.Assert(referencedCompilation.IsCompiled, "A referenced compilation wasnt successful",
                    $"path:'{path}', compileDirection {compileDirection}\n{string.Join(",", scriptErrors.Select(s => s.ToString()))}\n\n\n");

                if (referencedCompilation.IsCompiled)
                {
                    var assembly = Assembly.LoadFile(referencedCompilation.AssemblyFilePath);
                    namespaces.AddRange(referencedCompilation.FoundNamespaces);
                    metadataReferences.AddRange(referencedCompilation.FoundMetadataReferences);
                    scriptLoadsAsAssemblies.Add(loadDirective.ToString(), assembly);

                    executingScriptCompilationSource = executingScriptCompilationSource.RemoveNode(loadDirective, SyntaxRemoveOptions.KeepNoTrivia);
                }
                else
                {
                    return new ScriptDebugResult(null, scriptErrors);
                }
            }

            var wholeUnit = CompilationHelpers.WrapScriptInStandardClass(executingScriptCompilationSource, "ScriptyDebugNs", "ScriptyDebugCls",
                "ScriptyDebugMeth", source.FilePath);

            var formatted = Formatter.Format(wholeUnit, new AdhocWorkspace());

            compilationSources.Add(formatted.SyntaxTree);

            //errors(4, 22 - The type or namespace name 'Scripty.Core.Output.OutputFileCollection' could not be 
            //found(are you missing a using directive or an assembly reference?) \r\n(4,76 - The type or namespace name 'Scripty.Core.Output.OutputFileCollection' could not be found(are you missing a using directive or an assembly reference?) \r\n

            var context = GetContext(source.FilePath);
            var outputDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptFileName = Path.GetFileName(source.FilePath);
            var outputPath = FileUtilities.BuildFullPath(outputDir, scriptFileName);
            var detailsToUseForTarget = CsRewriter.GetRewriteAssemblyPaths(outputPath);

            try
            {
                var compResult = CompilationHelpers.CompileAndWriteAssembly(source.FilePath,
                    compilationSources.ToImmutableList(), detailsToUseForTarget,
                    namespaces, metadataReferences);


                if (compResult.IsCompiled == false)
                {
                    return new ScriptDebugResult(null, BuildScriptErrors(compResult.CompilationResult.Diagnostics));
                }
            }
            finally
            {
                context.Dispose();
            }

            //then load the assembly and pdb
            //var entry = asm.EntryPoint;

            //then invoke its entry point (what is that for a script?)


            //then Process.Attach. 
            //  this may need Debugger.Launch() or Break() injected pre compilation


            //CSharpScript.Create(source.Code, options, typeof(ScriptContext));
            return new ScriptDebugResult(null, new List<ScriptError>());
        }

        /// <summary>
        /// Extracted as may be needed
        /// </summary>
        /// <param name="scriptLoadsAsAssemblies">The script loads as assemblies.</param>
        /// <param name="executingScriptCompilationSource">The executing script compilation source.</param>
        // ReSharper disable UnusedMember.Local
        private static void ReplaceDirectives(Dictionary<string, Assembly> scriptLoadsAsAssemblies,
            // ReSharper restore UnusedMember.Local
            CompilationUnitSyntax executingScriptCompilationSource)
        {
            foreach (var recompiledAsm in scriptLoadsAsAssemblies)
            {
                //assembliesToRef.Add(recompiledAsm.Value);
                var existingLoadDirective = executingScriptCompilationSource.GetLoadDirectives().Single(f => f.ToString() == recompiledAsm.Key);
                var rawFileValue = $"{recompiledAsm.Value.Location}";
                var literalExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(rawFileValue))
                    .WithLeadingTrivia(SyntaxFactory.Space);
                var replacementDirective = SyntaxFactory.ReferenceDirectiveTrivia(literalExpression.Token, true)
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                executingScriptCompilationSource = executingScriptCompilationSource.ReplaceNode(existingLoadDirective, replacementDirective);
            }
        }

        //This needs to move to the CustomTool
        public static void AttachCurrentVsDebugger()
        {
            //var envDte = new DteHelper().GetDte();
            //envDte.Debugger.CurrentProcess.Attach(); //nah, the one we launch
            //foreach (var process in envDte.Debugger.LocalProcesses)
            //{
            //    var p = process as Process;
            //    if (process.Name.IndexOf("devenv.exe"))
            //    {
            //        process.Attach();
            //        break;
            //    }
            //}
        }
    }
}