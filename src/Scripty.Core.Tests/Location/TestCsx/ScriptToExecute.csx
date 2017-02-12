#r ".\..\..\..\packages\NUnit.3.4.0\lib\net45\nunit.framework.dll"
#load "..\TestCs\ReferencedClass.cs"
#load "ReferencedScript.csx"

//perhaps we can do something for the packages stuff. I remember seeing there
//was some api surfaces available to locate (and get) packages.
Output.WriteLine("namespace TestNamespace{class TestClass{public void TestMethod(){}}}");