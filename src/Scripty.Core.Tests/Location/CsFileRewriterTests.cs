namespace Scripty.Core.Tests.Location
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using NUnit.Framework;
    using Resolvers;

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
            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }


        [Test]
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
            var asmList = new List<Assembly> {asm};

            //dirty loader rewrite. Would be nice if had result.OriginalDirectivePath
            var callingScript = CsxTestHelpers.GetFileContent(_scriptFileToExecute);
            var rewrittenScript = callingScript.Replace("#load \"..\\TestCs\\ReferencedClass.cs\"",
                $"#r \"{result.AssemblyFilePath}\"");
            var rewrittenScriptFileName = $"{_scriptFileToExecute}.rewrite.csx";
            CsxTestHelpers.WriteFileContent(rewrittenScriptFileName, rewrittenScript);


            var runResult = CreateAndRunScriptFile(rewrittenScriptFileName, asmList);
            Assert.IsNotNull(runResult);
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


        private static List<string> ExpandNamespace(string trimStartNamespace)
        {
            var namespaceValue = trimStartNamespace.Replace("namespace", string.Empty).Trim();
            var endRemoved = namespaceValue.Split(' ');
            var parts = endRemoved[0].Split('.');
            var partsAsBuilt = new StringBuilder();
            var returnList = new List<string>();
            foreach (var part in parts)
            {
                partsAsBuilt.Append(part);
                returnList.Add(partsAsBuilt.ToString());
                partsAsBuilt.Append(".");
            }
            return returnList;
        }

        private ScriptResult CreateAndRunScriptFile(string scriptFilePath, List<Assembly> otherAssemblies = null)
        {

            var se = new ScriptEngine(CsTestHelpers.ProjectFilePath);
            var ss = new ScriptSource(scriptFilePath, CsxTestHelpers.GetFileContent(scriptFilePath));
            var result = se.Evaluate(ss).Result;
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