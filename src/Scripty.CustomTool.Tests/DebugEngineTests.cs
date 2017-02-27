namespace Scripty.CustomTool.Tests
{
    using System;
    using System.Linq;
    using Core;
    using NUnit.Framework;

    [TestFixture]
    public class ScriptDebuggingTest : TracedBaseFixture
    {

        public TestHelpers CsTestHelpers { get; set; } = new  TestHelpers("TestCs");
        public TestHelpers CsxTestHelpers { get; set; } = new TestHelpers("TestCsx");

        private string _scriptFileToExecute;
        
        [Test]
        public void _TestAsmCreation()
        {
            var se = CsxTestHelpers.BuildDebugEngine();
            var ss = new ScriptSource(_scriptFileToExecute, CsxTestHelpers.GetFileContent(_scriptFileToExecute));
            var result = se.DebugScript(ss);

            Assert.IsNotNull(result);
            var errs = string.Join(string.Empty, result.Errors.Select(e => $"{e} {Environment.NewLine}"));
            Assert.AreEqual(0, result.Errors.Count, $"errors {errs}");


            Assert.IsTrue(false, "Test completed without errors. Doesnt mean it works tho");
        }

        public override void OnOneTimeSetup()
        {
            _scriptFileToExecute = CsxTestHelpers.GetTestFilePath("ScriptToExecute.csx");


            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        public override void OnOneTimeTearDown()
        {
            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        /*
         * 
         * Test Name:	_TestAsmCreation
Test FullName:	Scripty.CustomTool.Tests.ScriptDebuggingTest._TestAsmCreation
Test Source:	E:\Projects\Scripty\src\Scripty.CustomTool.Tests\DebugEngineTests.cs : line 19
Test Outcome:	Failed
Test Duration:	0:01:05.566

Result StackTrace:	at Scripty.CustomTool.Tests.ScriptDebuggingTest._TestAsmCreation() in E:\Projects\Scripty\src\Scripty.CustomTool.Tests\DebugEngineTests.cs:line 29
Result Message:	
Test completed without errors. Doesnt mean it works tho
  Expected: True
  But was:  False





         */
    }
}