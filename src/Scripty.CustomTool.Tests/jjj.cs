namespace ScriptyDebugNs
{
    public static class ScriptyDebugCls
    {
        public static Scripty.Core.Output.OutputFileCollection Output = new Scripty.Core.Output.OutputFileCollection("E:\\Projects\\Scripty\\src\\Scripty.CustomTool.Tests\\TestCsx\\ScriptToExecute.csx");

        public static void ScriptyDebugMeth()
        {
            //#r ".\..\..\packages\NUnit.3.4.0\lib\net45\nunit.framework.dll"
            //#load "..\TestCs\ReferencedClass.cs"
            //#load "ReferencedScript.csx"

            //Write using supplied ScriptContext
            Output.WriteLine("namespace TestNamespace{class TestClass{public void TestMethod(){}}}");
            var myString = "thisValue";
            var replaced = myString.Replace("this", "that");
        }
    }
}