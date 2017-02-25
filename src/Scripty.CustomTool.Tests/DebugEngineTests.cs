namespace Scripty.CustomTool.Tests
{
    using System;
    using System.Linq;
    using Core;
    using NUnit.Framework;

    [TestFixture]
    public class ScriptDebuggingTest
    {

        public TestHelpers CsTestHelpers { get; set; } = new  TestHelpers("TestCs");
        public TestHelpers CsxTestHelpers { get; set; } = new TestHelpers("TestCsx");

        private string _scriptFileToExecute;


        [OneTimeSetUp]
        public void Setup()
        {
            _scriptFileToExecute = CsxTestHelpers.GetTestFilePath("ScriptToExecute.csx");


            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            CsTestHelpers.RemoveFiles("*.rewrite.*");
        }

        [Test]
        public void _TestAsmCreation()
        {
            var se = CsxTestHelpers.BuildDebugEngine();
            var ss = new ScriptSource(_scriptFileToExecute, CsxTestHelpers.GetFileContent(_scriptFileToExecute));
            var result = se.DebugScript(ss);

            Assert.IsNotNull(result);
            var errs = string.Join(string.Empty, result.Errors.Select(e => $"{e} {Environment.NewLine}"));
            Assert.AreEqual(0, result.Errors.Count, $"errors {errs}");
        }
        /*
         * 
         * Got this to load compiled scripts and pdb's. There is some other problem. Not sure what. this is from output window
         * 
         * Maybe the new dll have other namespaces needing import
         * 
       'vstest.executionengine.x86.exe' (CLR v4.0.30319: domain-ee3c3b5a-Scripty.Core.Tests.dll): Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.Text.Encoding.Extensions\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Text.Encoding.Extensions.dll'. Cannot find or open the PDB file.
The thread 0x3080 has exited with code 0 (0x0).
Exception thrown: 'System.DllNotFoundException' in Microsoft.CodeAnalysis.dll
'vstest.executionengine.x86.exe' (CLR v4.0.30319: domain-ee3c3b5a-Scripty.Core.Tests.dll): Loaded 'E:\Projects\Scripty\src\Scripty.Core.Tests\Compilation\TestCs\ReferencedClass.cs.zplabc44.v1o.rewrite.dll'. Symbols loaded.
The thread 0x2be4 has exited with code 0 (0x0).
'vstest.executionengine.x86.exe' (CLR v4.0.30319: domain-ee3c3b5a-Scripty.Core.Tests.dll): Loaded 'E:\Projects\Scripty\src\Scripty.Core.Tests\Compilation\TestCsx\ReferencedScript.csx.em0no1nh.nzy.rewrite.dll'. Symbols loaded.
Exception thrown: 'System.NullReferenceException' in Microsoft.CodeAnalysis.CSharp.dll
Exception thrown: 'System.NullReferenceException' in mscorlib.dll

         */
    }
}