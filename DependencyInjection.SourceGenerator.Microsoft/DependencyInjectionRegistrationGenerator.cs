using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using DependencyInjection.SourceGenerator.Shared;
using DependencyInjection.SourceGenerator.Microsoft.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.Microsoft;

public record RegistrationExtension(string ClassFullName, string MethodName, List<Diagnostic> Errors);

[Generator]
public class DependencyInjectionRegistrationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassAttributeReceiver(additionalMethodAttributes: [nameof(RegistrationExtensionAttribute)]));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var @namespace = "Microsoft.Extensions.DependencyInjection";
        var safeAssemblyName = EscapeAssemblyNameToMethodName(context.Compilation.AssemblyName);
        var extensionName = "Add" + safeAssemblyName;

        var classesToRegister = RegistrationCollector.GetTypes(context);
        var registerAllTypes = RegistrationCollector.GetRegisterAllTypes(context);

        var source = GenerateExtensionMethod(context, extensionName, @namespace, classesToRegister, registerAllTypes);
        var sourceText = source.ToFullString();
        context.AddSource("ServiceCollectionExtensions.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    public static string EscapeAssemblyNameToMethodName(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return "Default";

        var sb = new StringBuilder();
        var ensureNextUpper = true;
        foreach (var c in assemblyName!)
        {
            if (char.IsLetterOrDigit(c))
            {
                var letter = c;
                if (ensureNextUpper)
                {
                    letter = char.ToUpperInvariant(c);
                    ensureNextUpper = false;
                }
                sb.Append(letter);
            }
            else
            {
                ensureNextUpper = true;
                continue;
            }
        }
        return sb.ToString();
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

    private static CompilationUnitSyntax GenerateExtensionMethod(GeneratorExecutionContext context, string extensionName, string @namespace, IEnumerable<INamedTypeSymbol> classesToRegister, IEnumerable<Registration> additionalRegistrations)
    {
        var bodyMembers = new List<ExpressionStatementSyntax>();

        foreach (var type in classesToRegister)
        {
            var registrations = RegistrationMapper.CreateRegistration(type);
            foreach (var registration in registrations)
            {
                bodyMembers.Add(CreateRegistrationSyntax(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));                
            }

            var decoration = DecorationMapper.CreateDecoration(type);
            if (decoration is not null)
                bodyMembers.Add(CreateDecorationSyntax(decoration.DecoratedTypeName, decoration.DecoratorTypeName));

            var registrationExtensions = CreateRegistrationExtensions(type);

            foreach (var registrationExtension in registrationExtensions)
            {
                if (!registrationExtension.Errors.Any())
                {
                    bodyMembers.Add(CreateRegistrationExtensionSyntax(registrationExtension.ClassFullName, registrationExtension.MethodName));
                    continue;
                }
                foreach (var error in registrationExtension.Errors)
                {
                    context.ReportDiagnostic(error);
                }
            }
        }

        foreach (var registration in additionalRegistrations)
        {
            bodyMembers.Add(CreateRegistrationSyntax(registration.ServiceType, registration.ImplementationTypeName, registration.Lifetime, registration.ServiceName));
        }

        var methodModifiers = SyntaxFactory.TokenList([SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)]);

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

        var methodDeclaration = SyntaxFactory.MethodDeclaration(serviceCollectionSyntax, SyntaxFactory.Identifier(extensionName))
                            .WithModifiers(SyntaxFactory.TokenList(methodModifiers))
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

        var classModifiers = SyntaxFactory.TokenList([SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword)]);
        var classDeclaration = SyntaxFactory.ClassDeclaration("ServiceCollectionExtensions")
                    .WithModifiers(classModifiers)
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

    internal static List<RegistrationExtension> CreateRegistrationExtensions(INamedTypeSymbol type)
    {
        var registrations = new List<RegistrationExtension>();
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method)
                continue;

            var attribute = TypeHelper.GetAttributes<RegistrationExtensionAttribute>(member.GetAttributes());
            if (!attribute.Any())
                continue;

            List<Diagnostic> errors = [];
            if (method.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal and not Accessibility.Friend)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DIM0001",
                        "Invalid method accessor",
                        "Method {0} on type {1} must be public or internal",
                        "InvalidConfig",
                        DiagnosticSeverity.Error,
                        true), null, method.Name, type.Name);
                errors.Add(diagnostic);
            }

            if (!method.IsStatic)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DIM0002",
                        "Method must be static",
                        "Method {0} on type {1} must be static",
                        "InvalidConfig",
                        DiagnosticSeverity.Error,
                        true), null, method.Name, type.Name);
                errors.Add(diagnostic);
            }

            if (method.Parameters.Length != 1)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DIM0002",
                        "Invalid parameter count",
                        "Method {0} on type {1} must have exactly one parameter of type IServiceCollection",
                        "InvalidConfig",
                        DiagnosticSeverity.Error,
                        true), null, method.Name, type.Name);
                errors.Add(diagnostic);
            }

            var firstParameter = method.Parameters.FirstOrDefault();
            if (firstParameter is not null && TypeHelper.GetFullName(firstParameter.Type) != "global::Microsoft.Extensions.DependencyInjection.IServiceCollection")
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DIM0002",
                        "Invalid parameter type",
                        "Method {0} on type {1} must have input parameter of type IServiceCollection",
                        "InvalidConfig",
                        DiagnosticSeverity.Error,
                        true), null, method.Name, type.Name);
                errors.Add(diagnostic);
            }

            var registration = new RegistrationExtension(TypeHelper.GetFullName(type), method.Name, errors);
            registrations.Add(registration);
        }
        return registrations;
    }

    private static ExpressionStatementSyntax CreateRegistrationExtensionSyntax(string className, string methodName)
    {
        var arguments = SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName("services"))));

        var expressionExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(className),
                SyntaxFactory.IdentifierName(methodName)))
        .WithArgumentList(arguments);

        return SyntaxFactory.ExpressionStatement(expressionExpression);
    }

    private static ExpressionStatementSyntax CreateDecorationSyntax(string decoratedTypeName, string decoratorTypeName)
    {
        SyntaxNodeOrToken[] tokens =
            [
                SyntaxFactory.IdentifierName(decoratedTypeName),
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.IdentifierName(decoratorTypeName)
            ];

        var accessExpression = SyntaxFactory.MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              SyntaxFactory.IdentifierName("services"),
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

    private static ExpressionStatementSyntax CreateRegistrationSyntax(string? serviceType, string implementation, Lifetime lifetime, string? serviceName)
    {
        var keyed = serviceName is null ? string.Empty : "Keyed";
        var lifetimeName = lifetime switch
        {
            Lifetime.Singleton => $"Singleton",
            Lifetime.Scoped => "Scoped",
            Lifetime.Transient => "Transient",
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
        var methodName = $"Add{keyed}{lifetimeName}";

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

        var accessExpression = SyntaxFactory.MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              SyntaxFactory.IdentifierName("services"),
              SyntaxFactory.GenericName(
                  SyntaxFactory.Identifier(methodName))
              .WithTypeArgumentList(
                  SyntaxFactory.TypeArgumentList(
                      SyntaxFactory.SeparatedList<TypeSyntax>(tokens))));

        var argumentList = SyntaxFactory.ArgumentList();
        if (serviceName is not null)
        {
            argumentList = SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(serviceName)))));

        }

        var expression = SyntaxFactory.InvocationExpression(accessExpression)
              .WithArgumentList(argumentList);

        return SyntaxFactory.ExpressionStatement(expression);
    }
}
