namespace Scripty.Core.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    /// <remarks>
    ///     Single threaded so the file existence evaluations are clear. 
    /// </remarks>
    [SingleThreaded]
    public class ScriptEngineTests : BaseFixture
    {
        #region "common test members"

        private static readonly string _simpleSuccessScriptFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleSuccess.csx");
        private static readonly string _simpleSuccessOutputFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleSuccess.cs");
        private static readonly string _simpleScriptKeepScriptFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleScriptKeep.csx");
        private static readonly string _simpleScriptKeepOutputFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleScriptKeep.cs");
        private static readonly string _simpleScriptIgnoreScriptFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleScriptIgnore.csx");
        private static readonly string _simpleScriptIgnoreOutputFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleScriptIgnore.cs");
        private static readonly string _simpleCompileFailureScriptFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleCompileFailure.csx");
        private static readonly string _simpleCompileFailureOutputFilePath = TestHelpers.GetFilePathRelativeToProjectRoot(".\\TestCsx\\SimpleCompileFailure.cs");

        private static void CleanupScriptOutputs()
        {
            var files = new List<string>();
            files.Add(_simpleSuccessOutputFilePath);
            files.Add(_simpleCompileFailureOutputFilePath);
            files.Add(_simpleScriptKeepOutputFilePath);
            files.Add(_simpleScriptIgnoreOutputFilePath);
            TestHelpers.RemoveFiles(files);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            CleanupScriptOutputs();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            CleanupScriptOutputs();
        }

        #endregion //#region "common test members"

        /// <summary>
        ///     Given the default output behavior is set and no errors occur
        ///     A file is placed in the output location, and its contents are overwritten by a successfully processed script
        /// </summary>
        [Test]
        public void Evaluate_Success_WithDefaultOnScriptSaveBehavior()
        {
            var ep = new EngineParams
            {
                ScriptFile = _simpleSuccessScriptFilePath,
                OutputFile = _simpleSuccessOutputFilePath,
                OutputFileCount = 1,
                ErrorCount = 0,
                OutContentMatchesTestContent = false
            };

            var result = EvaluateScriptAndGetResult(ep);

            Assert.IsNotNull(result);
            StringAssert.Contains("namespace TestNamespace{class TestClass{public void TestMethod(){}}}", result.DefaultOutputFileContents);
        }

        /// <summary>
        /// Given the default output behavior is set and compilation errors occur
        ///     A file is placed in the output location, and its contents are not overwritten by a successfully processed script
        /// </summary>
        [Test]
        public void Evaluate_CompilationFailureWithDefaultOutputBehavior()
        {
            var ep = new EngineParams
            {
                ScriptFile = _simpleCompileFailureScriptFilePath,
                OutputFile = _simpleCompileFailureOutputFilePath,
                OutputFileCount = 0,
                ErrorCount = 19,
                OutContentMatchesTestContent = true
            };

            var result = EvaluateScriptAndGetResult(ep);

            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Given the OutputBehavior is set to never generate output and no errors occur
        ///     A file is placed in the output location, and its contents are not overwritten by a successfully processed script
        /// </summary>
        [Test]
        public void Evaluate_Success_WithNeverGenerateOutput()
        {
            var ep = new EngineParams
            {
                ScriptFile = _simpleSuccessScriptFilePath,
                OutputFile = _simpleSuccessOutputFilePath,
                OutputFileCount = 1,
                ErrorCount = 0,
                OutContentMatchesTestContent = true,
                OutputBehavior = OutputBehavior.NeverGenerateOutput
            };

            var result = EvaluateScriptAndGetResult(ep);

            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Given the OutputBehavior is set to <see cref="OutputBehavior.ScriptControlsOutput"/>, the script is 
        ///     using <see cref="ScriptOutput.Keep"/> and no errors occur
        /// 
        ///     A file is placed in the output location, and its contents are overwritten by a successfully processed script
        /// </summary>
        [Test]
        public void Evaluate_Success_WithScriptControlsOutputKeep()
        {
            var ep = new EngineParams
            {
                ScriptFile = _simpleScriptKeepScriptFilePath,
                OutputFile = _simpleScriptKeepOutputFilePath,
                OutputFileCount = 1,
                ErrorCount = 0,
                OutContentMatchesTestContent = false,
                OutputBehavior = OutputBehavior.ScriptControlsOutput
            };

            var result = EvaluateScriptAndGetResult(ep);

            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Given the OutputBehavior is set to <see cref="OutputBehavior.ScriptControlsOutput"/>, the script is 
        ///     using <see cref="ScriptOutput.Ignore"/> and no errors occur
        /// 
        ///     A file is placed in the output location, and its contents are not overwritten by a successfully processed script
        /// </summary>
        [Test]
        public void Evaluate_Success_WithScriptControlsOutputIgnore()
        {
            var ep = new EngineParams
            {
                ScriptFile = _simpleScriptIgnoreScriptFilePath,
                OutputFile = _simpleScriptIgnoreOutputFilePath,
                OutputFileCount = 0,
                ErrorCount = 0,
                OutContentMatchesTestContent = true,
                OutputBehavior = OutputBehavior.ScriptControlsOutput
            };

            var result = EvaluateScriptAndGetResult(ep);

            Assert.IsNotNull(result);
        }


        public EngineResults EvaluateScriptAndGetResult(EngineParams ep)
        {
            CleanupScriptOutputs();
            FileAssert.DoesNotExist(ep.OutputFile);
            TestHelpers.CreateFiles(new List<string> { ep.OutputFile });
            FileAssert.Exists(ep.OutputFile);

            var scriptCode = TestHelpers.GetFileContent(ep.ScriptFile);
            var scriptSource = new ScriptSource(ep.ScriptFile, scriptCode);
            var se = TestHelpers.BuildScriptEngine();
            if (ep.OutputBehavior.HasValue)
            {
                se.OutputBehavior = ep.OutputBehavior.Value;
            }

            var result = se.Evaluate(scriptSource).Result;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Errors.Count == ep.ErrorCount, $"Unexpected Errors {result}");
            Assert.AreEqual(ep.OutputFileCount, result.OutputFiles.Count, $"Expected {ep.OutputFileCount} file but got {result.OutputFiles.Count}");
            FileAssert.Exists(ep.OutputFile);
            var content = TestHelpers.GetFileContent(ep.OutputFile);
            if (ep.OutContentMatchesTestContent == true)
            {
                StringAssert.AreEqualIgnoringCase(TestHelpers.TEST_FILE_CONTENT, content);
            }
            else
            {
                StringAssert.AreNotEqualIgnoringCase(TestHelpers.TEST_FILE_CONTENT, content);
            }

            return new EngineResults
            {
                DefaultOutputFileContents = content,
                ScriptResult = result
            };
        }



    }
}