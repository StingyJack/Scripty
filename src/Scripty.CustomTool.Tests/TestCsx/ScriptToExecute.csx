var myString = "thisValue";

//Write using supplied ScriptContext
Output.WriteLine("namespace TestNamespace{class TestClass{public void TestMethod(){}}}");

var replaced = myString.Replace("this", "that");
