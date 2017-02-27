namespace Scripty.Core.Compilation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Output;

    /// <summary>
    ///     Builds commonly used syntax elements
    /// </summary>
    public class SyntaxBuilder
    {
        #region "namespace"

        public static CompilationUnitSyntax NamespaceWrapper(string namespaceName, List<string> usingsList, MemberDeclarationSyntax[] members)
        {
            var usings = new List<UsingDirectiveSyntax>();
            foreach (var u in usingsList)
            {
                usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u).WithLeadingTrivia(SyntaxFactory.Space)));
            }

            return SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName)
                            .WithLeadingTrivia(SyntaxFactory.Space)
                            .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CarriageReturnLineFeed))
                        .AddUsings(usings.ToArray())
                        .AddMembers(members));
        }

        #endregion

        #region "class"

        public static ClassDeclarationSyntax ClassWrapper(string className, MemberDeclarationSyntax[] fieldMembers,
            MemberDeclarationSyntax[] methods)
        {
            return SyntaxFactory.ClassDeclaration(className)
                .AsPublic()
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
            var returnSyntax = returnType ?? ReturnVoid;
            var voidMain = SyntaxFactory.MethodDeclaration(returnSyntax, identifier)
                .WithBody(SyntaxFactory.Block(statements))
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CarriageReturnLineFeed);

            return voidMain;
        }

        public static PredefinedTypeSyntax ReturnVoid
        {
            get { return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)); }
        }

        public static SyntaxToken BuildMethodIdentifier(string methodName)
        {
            var methodIdentifier = SyntaxFactory.Identifier(SyntaxTriviaList.Create(SyntaxFactory.Space),
                methodName, SyntaxTriviaList.Create(SyntaxFactory.Space));
            return methodIdentifier;
        }

        #endregion //#region "method related"

        #region "member related"

        public static SyntaxTokenList ModifierPublic
        {
            get
            {
                return SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space));
            }
        }

        public static SyntaxTokenList ModifierStatic
        {
            get
            {
                return SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space));
            }
        }

        public static AttributeListSyntax AttributeSerializable
        {
            get
            {
                var list = new SeparatedSyntaxList<AttributeSyntax>();
                list = list.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(typeof(SerializableAttribute).Name)));
                var attrs = SyntaxFactory.AttributeList(list);
                return attrs;
            }
        }

        #endregion //#region "member related"

        #region "field"

        public static FieldDeclarationSyntax BuildOutputFileCollectionField(string scriptFilePath)
        {
            var outputFieldType = typeof(OutputFileCollection).Name;
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
                .AsPublic();
            return outputField;
        }

        #endregion //#region "field"
    }
}