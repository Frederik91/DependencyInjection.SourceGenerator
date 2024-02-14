using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using DependencyInjection.SourceGenerator.LightInject.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using DependencyInjection.SourceGenerator.Shared;

namespace DependencyInjection.SourceGenerator.LightInject;

[Generator]
public class DependencyInjectionRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //context.RegisterForSyntaxNotifications(() => new ClassAttributeReceiver(additionalClassAttributes: [nameof(RegisterCompositionRootAttribute)]));

        var receiver = new ClassAttributeReceiver(additionalClassAttributes: [nameof(RegisterCompositionRootAttribute)]);
        var classProvider = context.SyntaxProvider
                   .CreateSyntaxProvider((node, _) =>
                   {
                       if (node is not CompilationUnitSyntax compilationUnitSyntax)
                           return false;

                       foreach (var namespaceDeclarationSyntax in compilationUnitSyntax.Members.OfType<BaseNamespaceDeclarationSyntax>())
                       {
                           foreach (var classDeclarationSyntax in namespaceDeclarationSyntax.Members.OfType<ClassDeclarationSyntax>())
                           {
                               if (receiver.HasAttribute(classDeclarationSyntax))
                                   return true;
                           }
                       }

                       return false;
                   },
                   (ctx, _) =>
                   {
                       return (ctx.SemanticModel, (CompilationUnitSyntax)ctx.Node);
                   });

        context.RegisterSourceOutput(classProvider, Generate);
    }

    private void Generate(SourceProductionContext context, (SemanticModel, CompilationUnitSyntax) input)
    {
        var model = input.Item1;
        var compilation = input.Item2;
        // Get first existing CompositionRoot class
        var compositionRoot = compilation.SyntaxTree
            .GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(x => x.Identifier.Text == "CompositionRoot");

        if (compositionRoot is not null && !IsPartial(compositionRoot))
        {
            var descriptor = new DiagnosticDescriptor("DIL01", "CompositionRoot not patial", "CompositionRoot must be partial", "LightInject", DiagnosticSeverity.Error, true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, compositionRoot.GetLocation()));
            return;
        }

        // If CompositionRoot class exists, get namespace, if not, use root namespace of project
        var @namespace = GetDefaultNamespace(context, compositionRoot);

        var classesToRegister = RegistrationCollector.GetTypes(context);
        var registerAllTypes = RegistrationCollector.GetRegisterAllTypes(context);

        var source = GenerateCompositionRoot(context, compositionRoot is not null, @namespace, classesToRegister, registerAllTypes);
        var sourceText = source.ToFullString();
        context.AddSource("CompositionRoot.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    internal static string GetDefaultNamespace(SourceProductionContext context, ClassDeclarationSyntax? compositionRoot)
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

    private static CompilationUnitSyntax GenerateCompositionRoot(SourceProductionContext context, bool userDefinedCompositionRoot, string @namespace, IEnumerable<INamedTypeSymbol> classesToRegister, List<Registration> additionalRegistrations)
    {
        var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        if (userDefinedCompositionRoot)
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        var classModifiers = SyntaxFactory.TokenList(modifiers);

        var bodyMembers = new List<ExpressionStatementSyntax>();
        if (userDefinedCompositionRoot)
            bodyMembers.Add(CreateRegisterServicesCall());

        foreach (var type in classesToRegister)
        {
            var registration = RegistrationMapper.CreateRegistration(type);
            if (registration is not null)
                bodyMembers.Add(CreateServiceRegistration(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));

            var decoration = DecorationMapper.CreateDecoration(type);
            if (decoration is not null)
                bodyMembers.Add(CreateServiceDecoration(decoration.DecoratedTypeName, decoration.DecoratorTypeName));

            var registrationSyntax = CreateRegistrationExtensions(context, type);
            if (registrationSyntax is not null)
                bodyMembers.Add(registrationSyntax);

        }

        foreach (var registration in additionalRegistrations)
        {
            bodyMembers.Add(CreateServiceRegistration(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));
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

    internal static ExpressionStatementSyntax? CreateRegistrationExtensions(SourceProductionContext context, INamedTypeSymbol type)
    {
        var attribute = TypeHelper.GetAttributes<RegisterCompositionRootAttribute>(type.GetAttributes()).FirstOrDefault();
        if (attribute is null)
            return null;

        if (!TypeImplementsCompositionRoot(type))
        {
            var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                "DIL0002",
                "Invalid composition root implementation",
                "Class {0} does not implement ICompositionRoot",
                "InvalidConfig",
                DiagnosticSeverity.Error,
                true), null, type.Name);
            context.ReportDiagnostic(diagnostic);
            return null;
        }

        return CreateRegisterFromSyntax(type);
    }

    private static ExpressionStatementSyntax? CreateRegisterFromSyntax(INamedTypeSymbol type)
    {
        var typeName = TypeHelper.GetFullName(type);
        var invocationExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("serviceRegistry"),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("RegisterFrom"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(typeName))))));

        return SyntaxFactory.ExpressionStatement(invocationExpression);
    }

    private static bool TypeImplementsCompositionRoot(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(x => TypeHelper.GetFullName(x) == "global::LightInject.ICompositionRoot");
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

    private static bool IsPartial(ClassDeclarationSyntax compositionRoot)
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
