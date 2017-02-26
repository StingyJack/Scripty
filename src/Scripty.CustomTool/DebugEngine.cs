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
        public const string DEBUG_APP_DOMAIN = "ScriptyDebugAppDomain";
        public const string DEBUG_NAMESPACE_NAME = "ScriptyDebugNs";
        public const string DEBUG_CLASS_NAME = "ScriptyDebugCls";
        public const string DEBUG_METHOD_NAME = "ScriptyDebugMeth";
        

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
            Wt($"Begin debugging source '{source.FilePath}' with direction {compileDirection.Value}");
            
            //get the defaults and usual stuffs
            var referenceCollection = BuildReferenceCollection(additionalAssemblies);
            referenceCollection.Add(typeof(DebugEngine));
            var namespaces = BuildNamepspaces();
            var compilationSources = new List<SyntaxTree>();
            var scriptFolder = FileUtilities.GetDirectory(source.FilePath);
            var executingScriptParseOptions = CsRewriter.DefaultParseOptions; // CompilationHelpers.GetParseOptions(compileDirection.Value);

            var swStep = Stopwatch.StartNew();
            var executingScriptCompilationSource = CompilationHelpers.GetRootMainCompilationUnit(source.Code, executingScriptParseOptions);
            Wt($"Script main compilation unit extracted in {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            var refExtract = CompilationHelpers.ExtractReferenceDirectives(executingScriptCompilationSource, scriptFolder);
            executingScriptCompilationSource = refExtract.Item1;
            referenceCollection.Add(refExtract.Item2);
            Wt($"Script reference directives extracted in {swStep.ElapsedMilliseconds}ms");

            //this is the harder part. Each #load directive will need its content either compiled as 
            // a standard assembly with pdb, or as script assembly with pdb. Debugging requires pdb's.
            swStep.Restart();
            var loadDirectives = executingScriptCompilationSource.GetLoadDirectives();
            foreach (var loadDirective in loadDirectives)
            {
                var path = FileUtilities.BuildFullPath(scriptFolder, loadDirective.File.ValueText);
                var asmDetail = CsRewriter.GetRewriteAssemblyPaths(path);
                var referencedCompilation = CompilationHelpers.GetCompilationForLoadDirective(compileDirection.Value, path, asmDetail, 
                    namespaces, referenceCollection.AsMetadataReferences());
                var scriptErrors = BuildScriptErrors(referencedCompilation.CompilationResult.Diagnostics);

                Debug.Assert(referencedCompilation.IsCompiled, "A referenced compilation wasnt successful",
                    $"path:'{path}', compileDirection {compileDirection}\n{string.Join(",", scriptErrors.Select(s => s.ToString()))}\n\n\n");

                if (referencedCompilation.IsCompiled)
                {
                    namespaces.AddRange(referencedCompilation.FoundNamespaces);
                    referenceCollection.Add(referencedCompilation.FoundMetadataReferences);
                    //var assembly = Assembly.LoadFile(referencedCompilation.AssemblyFilePath);
                    //scriptLoadsAsAssemblies.Add(loadDirective.ToString(), assembly);

                    executingScriptCompilationSource = executingScriptCompilationSource.RemoveNode(loadDirective, SyntaxRemoveOptions.KeepNoTrivia);
                }
                else
                {
                    return new ScriptDebugResult(null, scriptErrors);
                }
            }
            Wt($"Script load directive removal took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            var wholeUnit = CompilationHelpers.WrapScriptInStandardClass(executingScriptCompilationSource, 
                DEBUG_NAMESPACE_NAME, DEBUG_CLASS_NAME, DEBUG_METHOD_NAME, source.FilePath);
            Wt($"Script wrapping took {swStep.ElapsedMilliseconds}ms");

            swStep.Restart();
            var formatted = Formatter.Format(wholeUnit, new AdhocWorkspace());
            Wt($"Script formatting took {swStep.ElapsedMilliseconds}ms");
            WriteBlockToTrace(formatted);
            compilationSources.Add(formatted.SyntaxTree);

            //errors(4, 22 - The type or namespace name 'Scripty.Core.Output.OutputFileCollection' could not be 
            //found(are you missing a using directive or an assembly reference?) \r\n(4,76 - The type or namespace name 'Scripty.Core.Output.OutputFileCollection' could not be found(are you missing a using directive or an assembly reference?) \r\n

            swStep.Restart();
            var context = GetContext(source.FilePath);
            var outputDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptFileName = Path.GetFileName(source.FilePath);
            var outputPath = FileUtilities.BuildFullPath(outputDir, scriptFileName);
            var detailsToUseForTarget = CsRewriter.GetRewriteAssemblyPaths(outputPath);
            Wt($"Precompilation details took {swStep.ElapsedMilliseconds}ms");

            try
            {
                swStep.Restart();
                var compResult = CompilationHelpers.CompileAndWriteAssembly(source.FilePath,
                    compilationSources.ToImmutableList(), detailsToUseForTarget,
                    namespaces, referenceCollection.AsMetadataReferences());
                Wt($"Script to Assembly compilation resulted in {compResult.IsCompiled} and took {swStep.ElapsedMilliseconds}ms");

                if (compResult.IsCompiled == false)
                {
                    return new ScriptDebugResult(null, BuildScriptErrors(compResult.CompilationResult.Diagnostics));
                }
            }
            finally
            {
                context.Dispose(); //text writer streams to close
            }

            swStep.Restart();
            var debugDomain = AppDomain.CreateDomain(DEBUG_APP_DOMAIN, null, AppDomain.CurrentDomain.SetupInformation);
            var compiledAssemblyName = AssemblyName.GetAssemblyName(detailsToUseForTarget.DllPath);
            var compiledAssembly = debugDomain.Load(compiledAssemblyName);
            var instances = compiledAssembly.GetTypes();
            var instance = debugDomain.CreateInstanceAndUnwrap(detailsToUseForTarget.AsmName, $"{DEBUG_NAMESPACE_NAME}.{DEBUG_CLASS_NAME}");
            Wt($"App domain creation and reference loading took {swStep.ElapsedMilliseconds}ms");
            /*
             Test Name:	_TestAsmCreation
Test FullName:	Scripty.CustomTool.Tests.ScriptDebuggingTest._TestAsmCreation
Test Source:	E:\Projects\Scripty\src\Scripty.CustomTool.Tests\DebugEngineTests.cs : line 35
Test Outcome:	Failed
Test Duration:	0:00:31.755

Result StackTrace:	
at System.Reflection.AssemblyName.nInit(RuntimeAssembly& assembly, Boolean forIntrospection, Boolean raiseResolveEvent)
   at System.Reflection.RuntimeAssembly.CreateAssemblyName(String assemblyString, Boolean forIntrospection, RuntimeAssembly& assemblyFromResolveEvent)
   at System.Activator.CreateInstance(String assemblyString, String typeName, Boolean ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityInfo, StackCrawlMark& stackMark)
   at System.Activator.CreateInstance(String assemblyName, String typeName)
   at System.AppDomain.CreateInstance(String assemblyName, String typeName)
   at System.AppDomain.CreateInstanceAndUnwrap(String assemblyName, String typeName)
   at System.AppDomain.CreateInstanceAndUnwrap(String assemblyName, String typeName)
   at Scripty.CustomTool.DebugEngine.DebugScript(ScriptSource source, IEnumerable`1 additionalAssemblies, Nullable`1 compileDirection) in E:\Projects\Scripty\src\Scripty.CustomTool\DebugEngine.cs:line 126
   at Scripty.CustomTool.Tests.ScriptDebuggingTest._TestAsmCreation() in E:\Projects\Scripty\src\Scripty.CustomTool.Tests\DebugEngineTests.cs:line 38
Result Message:	System.IO.FileLoadException : Could not load file or assembly 'E:\\Projects\\Scripty\\src\\Scripty.CustomTool.Tests\\bin\\Debug\\ScriptToExecute.csx.bq1ftp3m.0c4.rewrite.dll' or one of its dependencies. The given assembly name or codebase was invalid. (Exception from HRESULT: 0x80131047)


             */
             swStep.Restart();
            var processes = new DteHelper().GetDteVs14().Debugger.LocalProcesses.Cast<EnvDTE.Process>();
            var currentProcess = Process.GetCurrentProcess().Id;
            var process = processes.FirstOrDefault(p => p.ProcessID == currentProcess);
            process?.Attach();
            Wt($"Debugger process attach took {swStep.ElapsedMilliseconds}ms");

            //then invoke its entry point 

            return new ScriptDebugResult(null, new List<ScriptError>());
        }

        private void WriteBlockToTrace(SyntaxNode formatted)
        {
            Wt($"-------------BEGIN BLOCK-----------------");
            Wt($"{formatted.GetText().ToString()}");
            Wt($"-------------END BLOCK-----------------");
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