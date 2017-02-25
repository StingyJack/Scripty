namespace Scripty.Core.Compilation
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Output;

    /// <summary>
    ///     Builds commonly used syntax elements
    /// </summary>
    public static class SyntaxBuilder
    {
        #region "namespace"

        public static CompilationUnitSyntax NamespaceWrapper(string namespaceName, List<string> usingsList, MemberDeclarationSyntax[] members)
        {
            var usings = new List<UsingDirectiveSyntax>();
            foreach (var u in usingsList)
            {
                usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u)));
            }

            return SyntaxFactory.CompilationUnit()
                .AddUsings(usings.ToArray())
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName)
                            .WithLeadingTrivia(SyntaxFactory.Space)
                            .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CarriageReturnLineFeed))
                        .AddMembers(members));
        }

        #endregion

        #region "class"

        public static ClassDeclarationSyntax ClassWrapper(string className, MemberDeclarationSyntax[] fieldMembers,
            MemberDeclarationSyntax[] methods)
        {
            return SyntaxFactory.ClassDeclaration(className)
                .WithModifiers(ModifiersPublicStatic())
                .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CarriageReturnLineFeed)
                .AddMembers(fieldMembers)
                .AddMembers(methods)
                .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CarriageReturnLineFeed);
        }

        #endregion //#region "class"

        #region "method related"

        public static MethodDeclarationSyntax Method(string methodName, StatementSyntax statements, TypeSyntax returnType = null)
        {
            var identifier = BuildMethodIdentifier(methodName);
            var returnSyntax = returnType ?? ReturnVoid();
            var voidMain = SyntaxFactory.MethodDeclaration(returnSyntax, identifier)
                .WithBody(SyntaxFactory.Block(statements))
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CarriageReturnLineFeed);
            return voidMain;
        }

        public static PredefinedTypeSyntax ReturnVoid()
        {
            var returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            return returnType;
        }

        public static MethodDeclarationSyntax AsReturnVoid(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.WithReturnType(ReturnVoid());
        }

        public static SyntaxToken BuildMethodIdentifier(string methodName)
        {
            var methodIdentifier = SyntaxFactory.Identifier(SyntaxTriviaList.Create(SyntaxFactory.Space),
                methodName, SyntaxTriviaList.Create(SyntaxFactory.Space));
            return methodIdentifier;
        }

        #endregion //#region "method related"

        #region "common"

        public static SyntaxTokenList ModifiersPublicStatic()
        {
            var modifiers = SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space));
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space));
            return modifiers;
        }

        public static MethodDeclarationSyntax AsPublicStatic(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.WithModifiers(ModifiersPublicStatic());
        }

        #endregion //#region "common"

        #region "field"

        public static FieldDeclarationSyntax BuildOutputFileCollectionField(string scriptFilePath)
        {
            var outputFieldType = typeof(OutputFileCollection).FullName;
            var argList = SyntaxFactory.ArgumentList()
                .AddArguments(
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(scriptFilePath))));
            var outputField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(outputFieldType))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("Output"))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(outputFieldType))
                                    .WithArgumentList(argList)
                                    .WithNewKeyword(SyntaxFactory.Token(SyntaxKind.NewKeyword)))))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            return outputField;
        }

        #endregion //#region "field"
    }
}