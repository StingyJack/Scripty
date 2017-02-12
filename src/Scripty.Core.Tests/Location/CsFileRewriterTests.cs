namespace Scripty.Core.Tests.Location
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using NUnit.Framework;
    using Resolvers;

    [SingleThreaded]
    public class CsFileRewriterTests : BaseFixture
    {
        public TestHelpers CsTestHelpers { get; set; } = new TestHelpers("Location\\TestCs");
        public TestHelpers CsxTestHelpers { get; set; } = new TestHelpers("Location\\TestCsx");

        private string _scriptFileToExecute;
        private string _referencedClassFilePath;
        private string _referencedClassExpectedResultFilePath;

        [OneTimeSetUp] 
        public void Setup()
        {
            _scriptFileToExecute = CsxTestHelpers.GetTestFilePath("ScriptToExecute.csx");
            _referencedClassFilePath = CsTestHelpers.GetTestFilePath("ReferencedClass.cs");
            _referencedClassExpectedResultFilePath = CsTestHelpers.GetTestFilePath("ReferencedClass.ExpectedResult.cs");
            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        // probably going to nuke this way of doing things
        //[Test]
        public void RewriteReferencedClassFile()
        {
            var rewriteCandidate = new RewrittenFile
            {
                OriginalFilePath = _referencedClassFilePath,
                RewrittenFilePath = CsRewriter.GetRewriteFilePath(_referencedClassFilePath)
            };

            var result = CsRewriter.CreateRewriteFile(rewriteCandidate);

            var compareResult = CompareFiles(_referencedClassExpectedResultFilePath, result.RewrittenFilePath);

            Assert.IsTrue(compareResult.Item1, compareResult.Item2);

            var runResult = CreateAndRunScriptFile(result.RewrittenFilePath);
            Assert.IsNotNull(runResult);
        }

        [Test]
        public void RewriteReferencedClassFileAsAssembly()
        {
            var result = CsRewriter.CreateRewriteFileAsAssembly(_referencedClassFilePath);

            Assert.IsTrue(result.IsCompiled, "assembly was not compiled");

            Assert.IsTrue(FileUtilities.WriteAssembly(result.AssemblyFilePath, result.AssemblyBytes),
                "Failed to write assembly to disk");

            Assert.IsTrue(FileUtilities.WriteAssembly(result.PdbFilePath, result.PdbBytes),
                "Failed to write assembly pdb to disk");

            var asm = Assembly.LoadFile(result.AssemblyFilePath);
            var asmList = new List<Assembly>(result.FoundAssemblies) {asm};

            //dirty loader rewrite. Would be nice if had result.OriginalDirectivePath
            var callingScript = CsxTestHelpers.GetFileContent(_scriptFileToExecute);
            var rewrittenCallingScript = callingScript.Replace("#load \"..\\TestCs\\ReferencedClass.cs\"",
                $"#r \"{result.AssemblyFilePath}\"");
            var rewrittenScriptFileName = $"{_scriptFileToExecute}.rewrite.csx";
            CsxTestHelpers.WriteFileContent(rewrittenScriptFileName, rewrittenCallingScript);


            var runResult = CreateAndRunScriptFile(rewrittenScriptFileName, asmList, result.FoundNamespaces);
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

        /// <summary>
        ///     If you need to make changes to the expected result file, this will check its ability to compile
        /// </summary>
        [Test]
        public void EnsureExpectedResultFileStillCompiles()
        {
            var result = CreateAndRunScriptFile(_referencedClassExpectedResultFilePath);
            Assert.IsNotNull(result);
        }


        private ScriptResult CreateAndRunScriptFile(string scriptFilePath, List<Assembly> additionalAssemblies = null, List<string> additionalNamespaces = null)
        {

            var se = new ScriptEngine(CsTestHelpers.ProjectFilePath);
            var ss = new ScriptSource(scriptFilePath, CsxTestHelpers.GetFileContent(scriptFilePath));
            var result = se.Evaluate(ss, additionalAssemblies, additionalNamespaces).Result;
            return result;
        }

        private Tuple<bool, string> CompareFiles(string control, string experiment)
        {
            var controlLines = File.ReadAllLines(control);
            var experimentLines = File.ReadAllLines(experiment);

            var maxLineCount = Math.Max(controlLines.Length, experimentLines.Length);

            for (var i = 0; i < maxLineCount; i++)
            {
                var controlLine = controlLines[i];
                var experimentLine = experimentLines[i];

                if (string.Equals(controlLine, experimentLine, StringComparison.Ordinal))
                {
                    continue;
                }

                return new Tuple<bool, string>(false, $"lines at index {i} are not equal." +
                                                      $"Control '{controlLine}' " +
                                                      $"Experiment '{experimentLine}'");
            }
            return new Tuple<bool, string>(true, string.Empty);
        }
    }
}