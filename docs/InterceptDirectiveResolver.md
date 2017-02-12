## Intercept Directive Resolver
By default, the `SourceFileResolver` (derived from `SourceReferenceResolver`)  is called by `CSharpScript` when Creating, Running, or Evaluating scripts. 
Its job is to locate the targets in the `#r` and `#load` directives and provide those source files. 

An alternate implementaion of `SourceReferenceResolver` can be provided as part of the `ScriptOptions`. 

The `InterceptDirectiveResolver` is an alternate implementation that allows the script author to include a `.cs` class file as a `#load` directive.
For most uses, the top of the script would look something like this...

``` c#

#r ".\..\..\..\packages\NUnit.3.4.0\lib\net45\nunit.framework.dll"
#load "..\TestCs\ReferencedClass.cs"
#load "ReferencedScript.csx"

Output.WriteLine("namespace TestNamespace{class TestClass{public void TestMethod(){}}}");


```


This is done by 



``` mermaid
 graph TB
         subgraph engine
         a1-->a2
         end
         
         

```

```
@startuml
title Intercept Directive Resolver interactions

|Incoming feeders|
start
:Script that references .cs file>
:CSharp.ScriptOptions|
:Scripty.ScriptEngine|

|Main|
:CSharp.CSharpScript| 

|Interception|
:Scripty.InterceptDirectiveResolver\n\
  (// if the directive is for a **.cs**// \n\
  // file, the CsRewriter is called. // \n\
  // otherwise the directive is handled // \n\
  // by the default resolver//) |
:Scripty.CsRewriter|

|filesystem|
:temporary compilations and pdb\n\
  (//required because **CSharpScript** // \n\
  //needs Assembly.Location// )}

|Interception|
:Scripty.InterceptDirectiveResolver|

|Main|
:CSharp.CSharpScript|
:Output;
end

@enduml
```