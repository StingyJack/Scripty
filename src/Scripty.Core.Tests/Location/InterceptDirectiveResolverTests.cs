namespace Scripty.Core.Tests.Location
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Resolvers;

    /// <remarks>
    ///     Best tested through the engine
    /// </remarks>
    public class InterceptDirectiveResolverTests : BaseFixture
    {
        public TestHelpers CsTestHelpers { get; set; } = new TestHelpers("Location\\TestCs");
        public TestHelpers CsxTestHelpers { get; set; } = new TestHelpers("Location\\TestCsx");

        private string _validProjectFilePath;
        private string _scriptFileToExecute;
        private string _referencedClassFilePath;

        [OneTimeSetUp]
        public void Setup()
        {
            _validProjectFilePath = CsTestHelpers.ProjectFilePath;
            _scriptFileToExecute = CsxTestHelpers.GetTestFilePath("ScriptToExecute.csx");
            _referencedClassFilePath = CsTestHelpers.GetTestFilePath("ReferencedClass.csx");

            CsTestHelpers.RemoveFiles("*.rewrite.*");
            CsxTestHelpers.RemoveFiles("*.scriptytmp");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        [Test]
        public void ConstructionParamsTest()
        {
            var searchPaths = new List<string> { CsTestHelpers.ProjectFilePath, CsTestHelpers.GetTestFileSubFolder() };
            var basePath = AppContext.BaseDirectory;

            var idr = new InterceptDirectiveResolver(searchPaths.ToImmutableArray(), basePath);

            Assert.IsNotNull(idr);
        }

        [Test]
        public void ConstructionDefaultTest()
        {
            var idr = new InterceptDirectiveResolver();

            Assert.IsNotNull(idr);
        }

        [Test]
        public void EvaluateScript_WithoutAsmOrScriptOrClassRef()
        {


            Assert.IsTrue(false, "fill it out dummy");
        }

        [Test]
        public void EvaluateScript_WithAsm_ButNoScriptOrClassRef()
        {

            Assert.IsTrue(false, "fill it out dummy");
        }

        [Test]
        public void EvaluateScript_WithAsmAndScriptRef_ButNoClassRef()
        {

            Assert.IsTrue(false, "fill it out dummy");
        }

        [Test]
        public void EvaluateScript_WithScriptAndClassRef_ButNoAsmRef()
        {

            Assert.IsTrue(false, "fill it out dummy");
        }

        [Test]
        public void EvaluateScript_WithScriptRef_ButNoAsmOrClassRef()
        {

            Assert.IsTrue(false, "fill it out dummy");
        }

        [Test]
        public void EvaluateScript_WithdClassRef_ButNoAsmOrScriptRef()
        {

            Assert.IsTrue(false, "fill it out dummy");
        }

        [Test]
        public void EvaluateScript_WithAsmAndClassAndScriptRef()
        {
            var engine = CsxTestHelpers.BuildScriptEngine();
            var scriptCode = CsxTestHelpers.GetFileContent(_scriptFileToExecute);
            var scriptSource = new ScriptSource(_scriptFileToExecute, scriptCode);
            
            var result = engine.Evaluate(scriptSource).GetAwaiter().GetResult();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Errors.Count == 0, string.Join(Environment.NewLine,result.Errors.Select(e => $"{e.Line} {e.Column} {e.Message}")));
            var expectedResult = new List<string>
            {
                "namespace TestNamespace{class TestClass{public void TestMethod(){}}}",
                "// Emitting prop with backing field 69",
                "// using the referenced class to output - Value_"
            };
            var actualResult = CsxTestHelpers.GetFileLines(result.OutputFiles.Single().TargetFilePath);
            StringAssert.AreEqualIgnoringCase(expectedResult[0], actualResult[0]);
            StringAssert.AreEqualIgnoringCase(expectedResult[1], actualResult[1]);
            StringAssert.StartsWith(expectedResult[2], actualResult[2]);
            StringAssert.AreNotEqualIgnoringCase(expectedResult[2], actualResult[2]);
        }



        //for reference
        public void RewriteReferencedClassFileAsAssembly()
        {
            var result = CsRewriter.CreateRewriteFileAsAssembly(_referencedClassFilePath);

            Assert.IsTrue(result.IsCompiled, "assembly was not compiled");

            Assert.IsTrue(FileUtilities.WriteAssembly(result.AssemblyFilePath, result.AssemblyBytes),
                "Failed to write assembly to disk");

            Assert.IsTrue(FileUtilities.WriteAssembly(result.PdbFilePath, result.PdbBytes),
                "Failed to write assembly pdb to disk");

            var asm = Assembly.LoadFile(result.AssemblyFilePath);
            var asmList = new List<Assembly>(result.FoundAssemblies) { asm };

            //dirty rewrite. Would be nice if had result.OriginalDirectivePath
            var callingScript = CsxTestHelpers.GetFileContent(_scriptFileToExecute);
            var rewrittenCallingScript = callingScript.Replace("#load \"..\\TestCs\\ReferencedClass.cs\"",
                $"#r \"{result.AssemblyFilePath}\"");
            var rewrittenScriptFileName = $"{_scriptFileToExecute}.rewrite.csx";
            CsxTestHelpers.WriteFileContent(rewrittenScriptFileName, rewrittenCallingScript);


            var runResult = CsTestHelpers.EvaluateScript(rewrittenScriptFileName, asmList, result.FoundNamespaces);
            Assert.IsNotNull(runResult);
            var expectedResult = new List<string>
            {
                "namespace TestNamespace{class TestClass{public void TestMethod(){}}}",
                "// Emitting prop with backing field 69",
                "// using the referenced class to output - Value_"
            };
            var actualResult = CsxTestHelpers.GetFileLines(runResult.OutputFiles.Single().TargetFilePath);
            StringAssert.AreEqualIgnoringCase(expectedResult[0], actualResult[0]);
            StringAssert.AreEqualIgnoringCase(expectedResult[1], actualResult[1]);
            StringAssert.StartsWith(expectedResult[2], actualResult[2]);
            StringAssert.AreNotEqualIgnoringCase(expectedResult[2], actualResult[2]);
        }

        [Test]
        public void ParseDirectives()
        {
            var scriptSource = BuildSimpleValidScriptSource();

            var result = InterceptDirectiveResolver.ParseDirectives(scriptSource.FilePath);

            Assert.IsNotNull(result);
        }


        private ScriptSource BuildSimpleValidScriptSource()
        {
            return new ScriptSource(_scriptFileToExecute, CsxTestHelpers.GetFileContent(_scriptFileToExecute));
        }
    }
}