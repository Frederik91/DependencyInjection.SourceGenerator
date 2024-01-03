using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using DependencyInjection.SourceGenerator.Shared.Attributes;
using DependencyInjection.SourceGenerator.Shared.Enums;
using DependencyInjection.SourceGenerator.Shared;

namespace DependencyInjection.SourceGenerator.Microsoft;

[Generator]
public class DependencyInjectionRegistrationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassAttributeReceiver(nameof(RegisterAttribute)));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var @namespace = GetDefaultNamespace(context);
        var extensionName = "Add" + context.Compilation.Assembly.Name.Replace(".", "");

        var classesToRegister = RegistrationCollector.GetTypes(context);

        var source = GenerateExtensionMethod(extensionName, @namespace, classesToRegister);
        var sourceText = source.ToFullString();
        context.AddSource("ServiceCollectionExtensions.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    private static string GetDefaultNamespace(GeneratorExecutionContext context)
    {
        var @namespace = context.Compilation.SyntaxTrees
        .SelectMany(x => x.GetRoot().DescendantNodes())
        .OfType<NamespaceDeclarationSyntax>()
        .Select(x => x.Name.ToString())
        .Min();

        if (@namespace is not null)
            return @namespace;

        @namespace = context.Compilation.SyntaxTrees
        .SelectMany(x => x.GetRoot().DescendantNodes())
        .OfType<FileScopedNamespaceDeclarationSyntax>()
        .Select(x => x.Name.ToString())
        .Min();

        if (@namespace is not null)
            return @namespace;

        throw new NotSupportedException("Unable to calculate namespace");
    }

    public static CompilationUnitSyntax GenerateExtensionMethod(string extensionName, string @namespace, IEnumerable<INamedTypeSymbol> classesToRegister)
    {
        var modifiers = new List<SyntaxToken>{
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword) };

        var classModifiers = SyntaxFactory.TokenList(modifiers.ToArray());

        var bodyMembers = new List<ExpressionStatementSyntax>();

        foreach (var type in classesToRegister)
        {
            var registration = RegistrationMapper.CreateRegistration(type);
            if (registration is null)
                continue;

            //bodyMembers.Add(CreateRegistrationSyntax(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));
        }

        var body = SyntaxFactory.Block(bodyMembers.ToArray());



        var namespaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(@namespace));
        var trivia = CreateTrivia();
        var excludeFromCodeCoverageSyntax = CreateExcludeFromCodeCoverage();

        var serviceCollectionSyntax = SyntaxFactory.QualifiedName(
                                    SyntaxFactory.QualifiedName(
                                        SyntaxFactory.QualifiedName(
                                            SyntaxFactory.AliasQualifiedName(
                                                SyntaxFactory.IdentifierName(
                                                    SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                                                SyntaxFactory.IdentifierName("Microsoft")),
                                            SyntaxFactory.IdentifierName("Extensions")),
                                        SyntaxFactory.IdentifierName("DependencyInjection")),
                                    SyntaxFactory.IdentifierName("IServiceCollection"));

        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("services"));
        body = body.AddStatements(returnStatement);

        return SyntaxFactory.CompilationUnit()

            .WithMembers(
                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                    SyntaxFactory.ClassDeclaration("ServiceExtensions")
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            [
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                            ]))
                    .WithMembers(
                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                            SyntaxFactory.MethodDeclaration(serviceCollectionSyntax, SyntaxFactory.Identifier("AddQueryHandlers"))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    [
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                                    ]))
                            .WithParameterList(
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                        SyntaxFactory.Parameter(
                                            SyntaxFactory.Identifier("services"))
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.ThisKeyword)))
                                        .WithType(serviceCollectionSyntax))))
                             .WithBody(body)))))
                .NormalizeWhitespace();
    }

    private static SyntaxList<AttributeListSyntax> CreateExcludeFromCodeCoverage()
    {
        return SyntaxFactory.SingletonList(
    SyntaxFactory.AttributeList(
        SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.Attribute(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.AliasQualifiedName(
                                SyntaxFactory.IdentifierName(
                                    SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                                SyntaxFactory.IdentifierName("System")),
                            SyntaxFactory.IdentifierName("Diagnostics")),
                        SyntaxFactory.IdentifierName("CodeAnalysis")),
                    SyntaxFactory.IdentifierName("ExcludeFromCodeCoverage"))))));
    }

    private static SyntaxToken CreateTrivia()
    {
        return SyntaxFactory.Token(
                SyntaxFactory.TriviaList(
                    [
                        SyntaxFactory.Comment("// <auto-generated/>"),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.PragmaWarningDirectiveTrivia(
                                SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                                true)),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.NullableDirectiveTrivia(
                                SyntaxFactory.Token(SyntaxKind.EnableKeyword),
                                true))
                    ]),
                SyntaxKind.UsingKeyword,
                SyntaxFactory.TriviaList());
    }

    private static ExpressionStatementSyntax CreateRegisterServicesCall()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("RegisterServices"))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName("serviceRegistry"))))));
    }

    private static ExpressionStatementSyntax CreateRegistrationSyntax(string serviceType, string implementation, Lifetime lifetime, string? serviceName)
    {
        var lifetimeName = lifetime switch
        {
            Lifetime.Singleton => "PerContainerLifetime",
            Lifetime.Scoped => "PerScopeLifetime",
            Lifetime.Transient => "PerRequestLifeTime",
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                        new SyntaxNodeOrToken[]{
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.ObjectCreationExpression(
                                                            SyntaxFactory.IdentifierName("PerRequestLifetime"))
                                                        .WithArgumentList(
                                                            SyntaxFactory.ArgumentList())),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal("Test")))});

        var lifetimeSyntax = SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName(lifetimeName))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList()));

        var args = new List<SyntaxNodeOrToken>();
        if (!string.IsNullOrEmpty(serviceName))
        {
            var serviceNameSyntax = SyntaxFactory.Argument(
                                                       SyntaxFactory.LiteralExpression(
                                                           SyntaxKind.StringLiteralExpression,
                                                           SyntaxFactory.Literal(serviceName!)));
            args.Add(serviceNameSyntax);
            args.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
        }
        args.Add(lifetimeSyntax);

        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(args));

        SyntaxNodeOrToken[] tokens;
        if (serviceType == implementation)
        {
            tokens = [SyntaxFactory.IdentifierName(implementation)];
        }
        else
        {
            tokens =
            [
                SyntaxFactory.IdentifierName(serviceType),
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.IdentifierName(implementation)
            ];
        }

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("serviceRegistry"),
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Register"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                tokens)))))
             .WithArgumentList(argumentList));
    }
}
