namespace Scripty.Core.Compilation
{
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public enum CompileDirection
    {
        EverythingBuiltAsClassesAndReffed,
        OnlyClassesBuiltAsScriptsAndReffed
    }

    public static class Enumstensions
    {
        /// <summary>
        ///     Converts the assemblies to metadata references.
        /// </summary>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns></returns>
        public static IEnumerable<MetadataReference> ToMetadataReferences(this IEnumerable<Assembly> assemblies)
        {
            var result = new List<MetadataReference>();

            foreach (var assembly in assemblies)
            {
                result.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            return result;
        }


    }
}