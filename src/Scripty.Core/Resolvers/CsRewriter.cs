namespace Scripty.Core.Resolvers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    ///     Provides the means for creating alternate forms of a c# class file for 
    /// use in scripting
    /// </summary>
    public class CsRewriter
    {
        private static readonly CSharpParseOptions _defaultParseOptions = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
        
        /// <summary>
        ///     Creates a copy of the original file, without the things the <see cref="CSharpScript"/> resolver
        /// doesnt like.
        /// </summary>
        /// <param name="rewriteCandidate">The rewritten file.</param>
        /// <returns>null if the operation could not succeed</returns>
        /// <remarks>
        ///     This is rudimentary and probably got lots of bugs for edge 
        /// cases (like when you put classes in several namespaces in the same
        /// code file, or use egyptian bracing). 
        /// </remarks>
        public static RewrittenFile CreateRewriteFile(RewrittenFile rewriteCandidate)
        {
            FileUtilities.RemoveIfPresent(rewriteCandidate.RewrittenFilePath);

            var targetFileStream = new StreamWriter(rewriteCandidate.RewrittenFilePath);

            try
            {
                using (var sr = new StreamReader(rewriteCandidate.OriginalFilePath))
                {
                    //maybe there is a better way to do this aside from counting braces?
                    //http://stackoverflow.com/questions/32769630/how-to-compile-a-c-sharp-file-with-roslyn-programmatically
                    var braceDepth = 0;

                    var inBlockComment = false;
                    var inNamespace = false;

                    while (sr.EndOfStream == false)
                    {
                        var line = sr.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        var trimStart = line.TrimStart();
                        var trimEnd = line.TrimEnd();

                        if (trimStart.StartsWith("//"))
                        {
                            targetFileStream.WriteLine(line);
                            continue;
                        }

                        // while in block comments, dont count bracing
                        if (trimStart.StartsWith("/*"))
                        {
                            inBlockComment = true;
                            targetFileStream.WriteLine(line);
                            continue;
                        }

                        if (inBlockComment)
                        {
                            if (trimEnd.EndsWith("*/"))
                            {
                                inBlockComment = false;
                            }
                            targetFileStream.WriteLine(line);
                            continue;
                        }

                        var openingBraceCountForThisLine = trimStart.Length - trimStart.Replace("{", string.Empty).Length;
                        var closingBraceCountForThisLine = trimStart.Length - trimStart.Replace("}", string.Empty).Length;

                        // ReSharper disable StringIndexOfIsCultureSpecific.1
                        if (trimStart.IndexOf("namespace") >= 0)
                            // ReSharper restore StringIndexOfIsCultureSpecific.1
                        {
                            var stackedNamespaces = BuildStackedNamespacePaths(trimStart);
                            targetFileStream.WriteLine(stackedNamespaces.Select(n => $"using {n};"));
                            inNamespace = true;
                            braceDepth += openingBraceCountForThisLine;
                            braceDepth -= closingBraceCountForThisLine;
                            continue;
                        }

                        braceDepth += openingBraceCountForThisLine;
                        braceDepth -= closingBraceCountForThisLine;
                        //bool anythingBetweenBraces; //no nasty one liners

                        if (inNamespace && openingBraceCountForThisLine > 0
                            && braceDepth == 1
                            && closingBraceCountForThisLine < openingBraceCountForThisLine)
                        {
                            // "{", "{{", "{{ }", etc
                            targetFileStream.WriteLine();
                            continue;
                        }

                        if (inNamespace && closingBraceCountForThisLine > 0
                            && braceDepth == 0
                            && closingBraceCountForThisLine > openingBraceCountForThisLine)
                        {
                            // "}", "}}}", "{ }}", etc
                            //targetFileStream.WriteLine();
                            continue;
                        }

                        targetFileStream.WriteLine(line);
                    } //while reading stream
                } //using streamreader
            }
            finally
            {
                targetFileStream.Flush();
                targetFileStream.Close();
            }

            return rewriteCandidate;
        }

        /// <summary>
        ///  Extracts the class declarations from the namespaces in the original file and compiles the result.
        /// </summary>
        /// <param name="rewriteCandidateFilePath">The rewrite candidate file path.</param>
        /// <returns>
        /// The compiled result and pdb bytes, along with the suggested file names, but
        /// does not save the file to disk
        /// </returns>
        public static RewrittenAssembly CreateRewriteFileAsAssembly(string rewriteCandidateFilePath)
        {
            var rewriteCandidate = new RewrittenAssembly {OriginalFilePath = rewriteCandidateFilePath};
            
            var mainCompilationUnit = GetRootMainCompilationUnit(rewriteCandidateFilePath);
            if (mainCompilationUnit == null)
            {
                return rewriteCandidate;
            }

            var namespaceMembersToCompile = new List<SyntaxTree>();
            var originalNamespaces = new List<string>();
            var explicitUsings = new List<UsingDirectiveSyntax>();
            var referencedAssemblies = new List<Assembly>();
            explicitUsings.AddRange(mainCompilationUnit.Usings);

            var mcuNamespaces = mainCompilationUnit.Members.Where(m => m.IsKind(SyntaxKind.NamespaceDeclaration));
            foreach (var mcuNamespace in mcuNamespaces)
            {
                var namespaceDeclarationSyntax = mcuNamespace as NamespaceDeclarationSyntax;
                if (namespaceDeclarationSyntax == null)
                {
                    continue;
                }
                originalNamespaces.Add(namespaceDeclarationSyntax.Name.ToFullString());
                explicitUsings.AddRange(namespaceDeclarationSyntax.Usings.ToList());

                foreach (var member in namespaceDeclarationSyntax.Members)
                {
                    var msyntaxTree = CSharpSyntaxTree.ParseText(member.GetText(), _defaultParseOptions);
                    var memberRoot = msyntaxTree.GetRoot();
                    var mcu = memberRoot as CompilationUnitSyntax;
                    explicitUsings.AddRange(mcu.Usings.ToList());

                    var classDecls = mcu.Members.Where(c => c.IsKind(SyntaxKind.ClassDeclaration));

                    foreach (var classDecl in classDecls)
                    {
                        var ccu = classDecl.SyntaxTree.GetCompilationUnitRoot();
                        var classDeclSyntaxTree = CSharpSyntaxTree.Create(ccu, _defaultParseOptions);
                        namespaceMembersToCompile.Add(classDeclSyntaxTree);
                    }
                }
            }

            var executingAssembly = Assembly.GetExecutingAssembly();
            var callingAssembly = Assembly.GetCallingAssembly();
            referencedAssemblies.Add(executingAssembly);
            referencedAssemblies.Add(callingAssembly);

            var listOfUsings = GetListOfNamespaces(originalNamespaces, explicitUsings, executingAssembly, callingAssembly);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithUsings(listOfUsings);
            var metadataReferences = GetMetadataReferences(executingAssembly, callingAssembly);
            var rewriteAssemblyPaths = GetRewriteAssemblyPaths(rewriteCandidateFilePath);

            var compilation = CSharpCompilation.Create(
                //var compilation = CSharpCompilation.CreateScriptCompilation(
                rewriteAssemblyPaths.AsmName,
                namespaceMembersToCompile.ToArray(),
                metadataReferences.ToArray(),
                options
            );
            
            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                
                //success sometimes was false when there are warnings, but i didnt write it down
                // so maybe it was a specific kind. Or I should pay more attention.

                if (emitResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error) == 0)
                {
                    dllStream.Seek(0, SeekOrigin.Begin);
                    pdbStream.Seek(0, SeekOrigin.Begin);
                    rewriteCandidate.AssemblyBytes = dllStream.ToArray();
                    rewriteCandidate.PdbBytes = pdbStream.ToArray();
                    
                    rewriteCandidate.AssemblyFilePath = rewriteAssemblyPaths.DllPath;
                    rewriteCandidate.PdbFilePath = rewriteAssemblyPaths.PdbPath;
                    rewriteCandidate.CompilationResult = emitResult;
                    rewriteCandidate.FoundNamespaces.AddRange(listOfUsings);
                    rewriteCandidate.FoundAssemblies.AddRange(referencedAssemblies);
                }
            }
            return rewriteCandidate;
        }

        /// <summary>
        ///     Gets the main compilation unit for the root of the syntax tree. This should effectively be the namespace 'container'
        /// </summary>
        /// <param name="rewriteCandidateFilePath">The rewrite candidate file path.</param>
        /// <returns>null if unable to get the requested item</returns>
        private static CompilationUnitSyntax GetRootMainCompilationUnit(string rewriteCandidateFilePath)
        {
            var scriptCode = FileUtilities.GetFileContent(rewriteCandidateFilePath);
            var mainSyntaxTree = CSharpSyntaxTree.ParseText(scriptCode, _defaultParseOptions);

            SyntaxNode mainSyntaxTreeRoot;
            if (mainSyntaxTree.TryGetRoot(out mainSyntaxTreeRoot) == false)
            {
                return null;
            }
            
            var mainCompilationUnit = mainSyntaxTreeRoot as CompilationUnitSyntax;
            if (mainCompilationUnit == null)
            {
                return null;
            }
            return mainCompilationUnit;
        }

        /// <summary>
        ///     Gets the metadata references.
        /// </summary>
        /// <param name="executingAssembly">The executing assembly.</param>
        /// <param name="callingAssembly">The calling assembly.</param>
        /// <returns></returns>
        private static List<MetadataReference> GetMetadataReferences(Assembly executingAssembly, Assembly callingAssembly)
        {
            var metadataReferences = new List<MetadataReference>();
            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(DataSet).Assembly.Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(ScriptContext).Assembly.Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(executingAssembly.Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(callingAssembly.Location));
            return metadataReferences;
        }

        private static List<string> GetListOfNamespaces(List<string> originalNamespaces, List<UsingDirectiveSyntax> usings, Assembly executingAssembly,
            Assembly callingAssembly)
        {
            var listOfUsings = new List<string>();
            foreach (var originalNamespace in originalNamespaces)
            {
                listOfUsings.AddRange(BuildStackedNamespacePaths(originalNamespace));
            }

            listOfUsings.AddRange(usings.Select(u => u.Name.ToString()).Distinct());

            var eaNamespaces =
                executingAssembly.GetTypes().Where(t => string.IsNullOrWhiteSpace(t.Namespace) == false).Select(t => t.Namespace).Distinct();
            foreach (var eans in eaNamespaces)
            {
                if (listOfUsings.Contains(eans))
                {
                    continue;
                }
                listOfUsings.Add(eans);
            }
            var caNamespaces =
                callingAssembly.GetTypes().Where(t => string.IsNullOrWhiteSpace(t.Namespace) == false).Select(t => t.Namespace).Distinct();
            foreach (var cans in caNamespaces)
            {
                if (listOfUsings.Contains(cans))
                {
                    continue;
                }
                listOfUsings.Add(cans);
            }

            return listOfUsings;
        }


        /// <summary>
        ///     Converts a namespace into a set of additive usings
        /// </summary>
        /// <param name="trimStartNamespace">The trim start namespace.</param>
        /// <returns></returns>
        /// <example>
        /// Given the line "namespace Company.Product.Application.Module"
        /// This returns "using Company;using Company.Product;.using Company.Product.Application;using Company.Product.Application.Module;"
        /// </example>
        private static List<string> BuildStackedNamespacePaths(string trimStartNamespace)
        {
            var namespaceValue = trimStartNamespace.Replace("namespace", string.Empty).Trim();
            var endRemoved = namespaceValue.Split(' ');
            var parts = endRemoved[0].Split('.');
            var partsAsBuilt = new StringBuilder();
            var returnValue = new List<string>();
            foreach (var part in parts)
            {
                partsAsBuilt.Append(part);
                returnValue.Add(partsAsBuilt.ToString());
                partsAsBuilt.Append(".");
            }
            return returnValue;
        }


        /// <summary>
        /// Gets the rewrite file path.
        /// </summary>
        /// <param name="normalizedPath">The normalized path.</param>
        /// <returns></returns>
        public static string GetRewriteFilePath(string normalizedPath)
        {
            return $"{normalizedPath}.{Path.GetRandomFileName()}.rewrite.tmp";
        }

        /// <summary>
        /// Gets the rewrite assembly paths.
        /// </summary>
        /// <param name="normalizedPath">The normalized path.</param>
        /// <returns></returns>
        public static AsmDetail GetRewriteAssemblyPaths(string normalizedPath)
        {
            var name = $"{Path.GetRandomFileName()}.rewrite";
            var basePath = $"{normalizedPath}.{name}";

            return new AsmDetail
            {
                AsmName = name,
                DllPath = $"{basePath}.dll",
                PdbPath = $"{basePath}.pdb"
            };
        }

    }
}