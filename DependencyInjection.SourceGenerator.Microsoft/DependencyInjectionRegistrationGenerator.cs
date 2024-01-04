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
        var bodyMembers = new List<ExpressionStatementSyntax>();

        foreach (var type in classesToRegister)
        {
            var registration = RegistrationMapper.CreateRegistration(type);
            if (registration is null)
                continue;

            bodyMembers.Add(CreateRegistrationSyntax(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));
        }

        var modifiers = SyntaxFactory.TokenList([SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)]);

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

        var methodDeclaration = SyntaxFactory.MethodDeclaration(serviceCollectionSyntax, SyntaxFactory.Identifier("AddQueryHandlers"))
                            .WithModifiers(SyntaxFactory.TokenList(modifiers))
                            .WithParameterList(
                                SyntaxFactory.ParameterList(
                                    SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                        SyntaxFactory.Parameter(
                                            SyntaxFactory.Identifier("services"))
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.ThisKeyword)))
                                        .WithType(serviceCollectionSyntax))));


        var body = SyntaxFactory.Block(bodyMembers.ToArray());
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("services"));
        body = body.AddStatements(returnStatement);

        methodDeclaration = methodDeclaration.WithBody(body);

        var classDeclaration = SyntaxFactory.ClassDeclaration("ServiceCollectionExtensions")
                    .WithModifiers(modifiers)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(methodDeclaration));

        var dependencyInjectionUsingDirective = SyntaxFactory.UsingDirective(
            SyntaxFactory.QualifiedName(
            SyntaxFactory.QualifiedName(
                SyntaxFactory.AliasQualifiedName(
                    SyntaxFactory.IdentifierName(
                        SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                    SyntaxFactory.IdentifierName("Microsoft")),
                SyntaxFactory.IdentifierName("Extensions")),
            SyntaxFactory.IdentifierName("DependencyInjection")));

        return Trivia.CreateCompilationUnitSyntax(classDeclaration, @namespace, [dependencyInjectionUsingDirective]);
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
        var accessExpression = SyntaxFactory.MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              SyntaxFactory.IdentifierName("services"),
              SyntaxFactory.GenericName(
                  SyntaxFactory.Identifier("AddTransient"))
              .WithTypeArgumentList(
                  SyntaxFactory.TypeArgumentList(
                      SyntaxFactory.SeparatedList<TypeSyntax>(
                          new SyntaxNodeOrToken[]{
                                SyntaxFactory.IdentifierName(serviceType),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.IdentifierName(implementation)}))));

        var expression = SyntaxFactory.InvocationExpression(accessExpression)
              .WithArgumentList(SyntaxFactory.ArgumentList());

        return SyntaxFactory.ExpressionStatement(expression);
    }
}
