using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using DependencyInjection.SourceGenerator.Shared;
using System.Diagnostics;

namespace DependencyInjection.SourceGenerator.LightInject;

[Generator]
public class DependencyInjectionRegistrationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassAttributeReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        //Debugger.Launch();
        // Get first existing CompositionRoot class
        var compositionRoot = context.Compilation.SyntaxTrees
            .SelectMany(x => x.GetRoot().DescendantNodes())
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(x => x.Identifier.Text == "CompositionRoot");

        if (compositionRoot is not null && !IsPartial(compositionRoot))
        {
            var descriptor = new DiagnosticDescriptor("CW.1", "CompositionRoot not patial", "CompositionRoot must be partial", "LightInject", DiagnosticSeverity.Error, true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, compositionRoot.GetLocation()));
            return;
        }

        // If CompositionRoot class exists, get namespace, if not, use root namespace of project
        var @namespace = GetDefaultNamespace(context, compositionRoot);

        var classesToRegister = RegistrationCollector.GetTypes(context);

        var source = GenerateCompositionRoot(compositionRoot is not null, @namespace, classesToRegister);
        var sourceText = source.ToFullString();
        context.AddSource("CompositionRoot.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    internal static string GetDefaultNamespace(GeneratorExecutionContext context, ClassDeclarationSyntax? compositionRoot)
    {
        if (compositionRoot?.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
            return namespaceDeclarationSyntax.Name.ToString();

        if (compositionRoot?.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
            return fileScopedNamespaceDeclarationSyntax.Name.ToString();

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

    public static CompilationUnitSyntax GenerateCompositionRoot(bool userdefinedCompositionRoot, string @namespace, IEnumerable<INamedTypeSymbol> classesToRegister)
    {
        var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        if (userdefinedCompositionRoot)
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        var classModifiers = SyntaxFactory.TokenList(modifiers);

        var bodyMembers = new List<ExpressionStatementSyntax>();
        if (userdefinedCompositionRoot)
            bodyMembers.Add(CreateRegisterServicesCall());

        foreach (var type in classesToRegister)
        {
            var registration = RegistrationMapper.CreateRegistration(type);
            if (registration is not null)
                bodyMembers.Add(CreateServiceRegistration(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));

            var decoration = DecorationMapper.CreateDecoration(type);
            if (decoration is not null)
                bodyMembers.Add(CreateServiceDecoration(decoration.DecoratedTypeName, decoration.DecoratorTypeName));

        }

        var body = SyntaxFactory.Block(bodyMembers.ToArray());


        var serviceRegistryType = CreateServiceRegistrySyntax("IServiceRegistry");

        var methodParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("serviceRegistry"))
                                            .WithType(serviceRegistryType);

        var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), SyntaxFactory.Identifier("Compose"))
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddParameterListParameters(methodParameter)
                                .WithBody(body);

        var compositionRootSyntax = CreateServiceRegistrySyntax("ICompositionRoot");
        var baseType = SyntaxFactory.SimpleBaseType(compositionRootSyntax);

        var classDeclaration = SyntaxFactory.ClassDeclaration("CompositionRoot")
                        .WithModifiers(classModifiers)
                        .AddBaseListTypes(baseType)
                        .AddMembers(methodDeclaration);

        return Trivia.CreateCompilationUnitSyntax(classDeclaration, @namespace);
    }

    private static ExpressionStatementSyntax CreateServiceDecoration(string decoratedTypeName, string decoratorTypeName)
    {
        SyntaxNodeOrToken[] tokens =
           [
               SyntaxFactory.IdentifierName(decoratedTypeName),
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.IdentifierName(decoratorTypeName)
           ];

        var accessExpression = SyntaxFactory.MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              SyntaxFactory.IdentifierName("serviceRegistry"),
              SyntaxFactory.GenericName(
                  SyntaxFactory.Identifier("Decorate"))
              .WithTypeArgumentList(
                  SyntaxFactory.TypeArgumentList(
                      SyntaxFactory.SeparatedList<TypeSyntax>(tokens))));

        var argumentList = SyntaxFactory.ArgumentList();

        var expression = SyntaxFactory.InvocationExpression(accessExpression)
              .WithArgumentList(argumentList);

        return SyntaxFactory.ExpressionStatement(expression);
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

    private static ExpressionStatementSyntax CreateServiceRegistration(string? serviceType, string implementation, Lifetime lifetime, string? serviceName)
    {
        var lifetimeName = lifetime switch
        {
            Lifetime.Singleton => "PerContainerLifetime",
            Lifetime.Scoped => "PerScopeLifetime",
            Lifetime.Transient => "PerRequestLifeTime",
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };


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

        var lifetimeIdentifierSyntax = CreateServiceRegistrySyntax(lifetimeName);
        var lifetimeSyntaxArgument = SyntaxFactory.Argument(
            SyntaxFactory.ObjectCreationExpression(lifetimeIdentifierSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList()));

        args.Add(lifetimeSyntaxArgument);

        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(args));

        SyntaxNodeOrToken[] tokens;
        if (serviceType is null)
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

    private bool IsPartial(ClassDeclarationSyntax compositionRoot)
    {
        return compositionRoot.Modifiers.Any(x => x.Text == "partial");
    }

    internal static QualifiedNameSyntax CreateServiceRegistrySyntax(string className)
    {
        return SyntaxFactory.QualifiedName(
            SyntaxFactory.AliasQualifiedName(
                SyntaxFactory.IdentifierName(
                    SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                SyntaxFactory.IdentifierName("LightInject")),
            SyntaxFactory.IdentifierName(className));

        //var attribute = SyntaxFactory.Attribute(name);
        //var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

        //return SyntaxFactory.SingletonList(attributeList);
    }
}
