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
         * Loads compiled dll, cant find entry point i guess. remove static and make it an instance. 
         * Also whre are the contents in ILSpy
         * 
         *Test Name:	_TestAsmCreation
Test FullName:	Scripty.CustomTool.Tests.ScriptDebuggingTest._TestAsmCreation
Test Source:	E:\Projects\Scripty\src\Scripty.CustomTool.Tests\DebugEngineTests.cs : line 19
Test Outcome:	Failed
Test Duration:	0:00:06.207

Result StackTrace:	
at System.RuntimeType.CreateInstanceImpl(BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, StackCrawlMark& stackMark)
   at System.Activator.CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes)
   at System.Activator.CreateInstance(String assemblyString, String typeName, Boolean ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityInfo, StackCrawlMark& stackMark)
   at System.Activator.CreateInstance(String assemblyName, String typeName)
   at System.AppDomain.CreateInstance(String assemblyName, String typeName)
   at System.AppDomain.CreateInstanceAndUnwrap(String assemblyName, String typeName)
   at System.AppDomain.CreateInstanceAndUnwrap(String assemblyName, String typeName)
   at Scripty.CustomTool.DebugEngine.DebugScript(ScriptSource source, IEnumerable`1 additionalAssemblies, Nullable`1 compileDirection) in E:\Projects\Scripty\src\Scripty.CustomTool\DebugEngine.cs:line 136
   at Scripty.CustomTool.Tests.ScriptDebuggingTest._TestAsmCreation() in E:\Projects\Scripty\src\Scripty.CustomTool.Tests\DebugEngineTests.cs:line 22
Result Message:	System.MissingMethodException : Constructor on type 'ScriptyDebugNs.ScriptyDebugCls' not found.



         */
    }
}