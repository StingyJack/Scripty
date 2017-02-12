#r ".\..\..\..\packages\NUnit.3.4.0\lib\net45\nunit.framework.dll"
#load "..\TestCs\ReferencedClass.cs"
#load "ReferencedScript.csx"

//Write using supplied ScriptContext
Output.WriteLine("namespace TestNamespace{class TestClass{public void TestMethod(){}}}");

//Create instance from recompiled assembly
var rc1 = new ReferencedClass(Context);
Output.WriteLine($"// Emitting prop with backing field {rc1.PropertyWithBackingField}");
rc1.Owl($"// using the referenced class to output")
