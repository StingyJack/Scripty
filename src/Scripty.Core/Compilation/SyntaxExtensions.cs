﻿namespace Scripty.Core.Compilation
{
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class SyntaxExtensions
    {
        public static ClassDeclarationSyntax AsPublic(this ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax.WithModifiers(SyntaxBuilder.ModifierPublic);
        }

        public static ClassDeclarationSyntax AsSeriallzable(this ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax.AddAttributeLists(SyntaxBuilder.AttributeSerializable);
        }

        public static MethodDeclarationSyntax AsPublic(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.AddModifiers(SyntaxBuilder.ModifierPublic.ToArray());
        }

        public static MethodDeclarationSyntax AsStatic(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.AddModifiers(SyntaxBuilder.ModifierStatic.ToArray());
        }

        public static FieldDeclarationSyntax AsPublic(this FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            return fieldDeclarationSyntax.WithModifiers(SyntaxBuilder.ModifierPublic);
        }

        public static FieldDeclarationSyntax AsStatic(this FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            return fieldDeclarationSyntax.WithModifiers(SyntaxBuilder.ModifierStatic);
        }

        public static MethodDeclarationSyntax AsReturnVoid(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.WithReturnType(SyntaxBuilder.ReturnVoid);
        }
    }
}