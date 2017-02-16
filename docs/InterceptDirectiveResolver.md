## Intercept Directive Resolver
By default, the `SourceFileResolver` (derived from `SourceReferenceResolver`)  is called by `CSharpScript` when Creating, Running, or Evaluating scripts. 
Its job is to locate the targets in the `#r` and `#load` directives and provide those source files. 

An alternate implementaion of `SourceReferenceResolver` can be provided as part of the `ScriptOptions`. 

The `InterceptDirectiveResolver` is an alternate implementation that allows the script author to include a `.cs` class file as a `#load` directive.
For most uses, the script would look something like this...

``` c#
#r ".\..\..\..\packages\NUnit.3.4.0\lib\net45\nunit.framework.dll"
#load "..\TestCs\ReferencedClass.cs"
#load "ReferencedScript.csx"

//Write using supplied ScriptContext
Output.WriteLine("namespace TestNamespace{class TestClass{public void TestMethod(){}}}");

//Create instance from recompiled assembly
var rc1 = new ReferencedClass(Context);
Output.WriteLine($"// Emitting prop with backing field {rc1.PropertyWithBackingField}");
rc1.Owl($"// using the referenced class to output")

```


Oh, look. A picture to show how this works (**Go build this dummy**)
![picture](images/intercept.png)


Mermaid is broken for my markdown editor, maybe this works for you...
``` mermaid
graph LR

cont-->CSEngine
subgraph C# Script Engine
  CSEngine["<b>Evaluate()<b/>"]--> mdr["<b>Metadata Resolution</b><br><i>is where referenced <br/>assemblies are <br/>located and loaded.<br/><br/> <b>Interception preprocessing</b> <br>provides some of this <br/>information </i>. "]
  mdr-->sdr["<b>Source Resolution</b><br/><i>is where script files<br/>are located and loaded.<br/><br/><b>InterceptDirectiveResolver</b><br/>uses the information collected<br/> by preprocessing to return <br/>the <b>.cs as .csx</b> compilations</i>"]
 sdr-->cscrguts["CSharpScript does internal work"]
end

 cscrguts-->outputmade["Output is made"]

 jrs["start"]-->Evaluate["Evaluate()"]
 subgraph Scripty ScriptEngine
  Evaluate["<b><i>Evaluate()</i></b>"]  
 end

 subgraph Preprocessing
Evaluate-->beginpre["Begin<br/>Preprocessing"]
  beginpre-->ExAsm["<b>InterceptionPreprocessor</b> <br/> <i>&nbsp;identifies references with <br/><b>.cs</b> extension</i>"]
 ExAsm-->CsRew["<b>CsRewriter</b> <i>compiles <br/>the <b>.cs</b> as <b>csx</b> (c# scripts) <br/>and collects any <br/>metadata needed for <br/>later injection or <br/>resolution </i>"]
 CsRew-->idr["<b>InterceptDirectiveResolver</b> <br/>&nbsp;<i>is given the collection of <br/>preprocessed content</i>"]
 idr-->comppre["Complete<br/>Preprocessing"]
 end 

comppre-->next




 classDef terminators fill:#a9f,stroke:#333,stroke-width:4px;
classDef orange fill:#f96,stroke:#333,stroke-width:4px;
 class jrs terminators;
 class next terminators;
class cont terminators;
class outputmade terminators;



```


