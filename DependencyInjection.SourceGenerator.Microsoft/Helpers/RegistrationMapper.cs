using DependencyInjection.SourceGenerator.Microsoft.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Helpers;
internal static class RegistrationMapper
{
    internal static List<ClassRegistration> CreateRegistration(INamedTypeSymbol type)
    {
        var attributes = TypeHelper.GetClassAttributes<RegisterAttribute>(type);

        if (!attributes.Any())
            return [];

        var result = new List<ClassRegistration>();
        foreach (var attribute in attributes)
        {

            var serviceType = TypeHelper.GetServiceType(type, attribute);
            if (serviceType is null)
                continue;

            var lifetime = TypeHelper.GetLifetimeFromAttribute(attribute) ?? ServiceLifetime.Transient;

            var serviceNameArgument = TypeHelper.GetAttributeValue(attribute, nameof(RegisterAttribute.ServiceName));

            var includeFactory = TypeHelper.GetAttributeValue(attribute, nameof(RegisterAttribute.IncludeFactory)) as bool? ?? false;

            // Get the value of the property
            var serviceName = serviceNameArgument?.ToString();

            var implementationTypeName = TypeHelper.GetFullName(type);

            if (TypeHelper.IsSameType(type, serviceType.Type))
            {
                implementationTypeName = serviceType.Name;
                serviceType = null;
            }

            var registration = new ClassRegistration
            {
                ImplementationTypeName = implementationTypeName,
                ImplementationTypeSymbol = type,
                Lifetime = lifetime,
                IncludeFactory = includeFactory,
                ServiceName = serviceName,
                ServiceType = serviceType?.Name,
                ServiceTypeSymbol = serviceType?.Type
            };
            result.Add(registration);
        }

        return result;
    }

    internal static List<MethodFactoryRegistration> CreateRegistrationFromMethod(IMethodSymbol methodSymbol)
    {
        var registrations = new List<MethodFactoryRegistration>();

        var attributes = TypeHelper.GetMethodAttributes<RegisterAttribute>(methodSymbol);
        foreach (var attribute in attributes)
        {
            var serviceTypeSymbol = TypeHelper.GetServiceTypeFromMethod(methodSymbol, attribute);
            var serviceType = serviceTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var lifetime = TypeHelper.GetLifetimeFromAttribute(attribute) ?? ServiceLifetime.Transient;
            var serviceName = TypeHelper.GetAttributeValue(attribute, "ServiceName") as string;

            registrations.Add(new MethodFactoryRegistration
            {
                ServiceType = serviceType,
                ServiceName = serviceName,
                Lifetime = lifetime,
                MethodClassName = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                MethodName = methodSymbol.Name
            });
        }

        return registrations;
    }

    public static (ExpressionStatementSyntax registrationExpression, ExpressionStatementSyntax? factoryExpression) CreateRegistrationSyntaxFromClass(string? serviceType, string implementation, ServiceLifetime lifetime, string? serviceName, bool includeFactory)
    {
        if (serviceType is not null)
        {
            serviceType = PrefixGlobalIfNotPresent(serviceType);
        }
        implementation = PrefixGlobalIfNotPresent(implementation);
        var keyed = serviceName is null ? string.Empty : "Keyed";
        var methodName = $"Add{keyed}{lifetime}";

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

        ExpressionStatementSyntax? factoryExpression = null;
        if (includeFactory)
            factoryExpression = CreateFactorySyntax(serviceType, implementation, lifetime, serviceName);
        return (SyntaxFactory.ExpressionStatement(expression), factoryExpression);
    }

    private static ExpressionStatementSyntax? CreateFactorySyntax(string? serviceType, string implementation, ServiceLifetime lifetime, string? serviceName)
    {
        var factoryMethodName = $"Add{(serviceName is null ? string.Empty : "Keyed")}{lifetime}";

        SyntaxNodeOrToken[] tokens;
        if (serviceType is null)
        {
            tokens = [SyntaxFactory.IdentifierName($"global::System.Func<{implementation}>")];
        }
        else
        {
            tokens =
            [
                SyntaxFactory.IdentifierName($"global::System.Func<{serviceType}>")
            ];
        }

        var accessExpression = SyntaxFactory.MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              SyntaxFactory.IdentifierName("services"),
              SyntaxFactory.GenericName(
                  SyntaxFactory.Identifier(factoryMethodName))
              .WithTypeArgumentList(
                  SyntaxFactory.TypeArgumentList(
                      SyntaxFactory.SeparatedList<TypeSyntax>(tokens))));

        var argumentList = SyntaxFactory.ArgumentList(
            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                SyntaxFactory.Argument(
                    SyntaxFactory.SimpleLambdaExpression(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("factory")),
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("factory"),
                            SyntaxFactory.GenericName(
                                SyntaxFactory.Identifier("GetRequiredService"))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(serviceType ?? implementation)))))))));

        if (serviceName is not null)
        {
            argumentList = argumentList.AddArguments(
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(serviceName))));
        }

        var expression = SyntaxFactory.InvocationExpression(accessExpression)
              .WithArgumentList(argumentList);

        return SyntaxFactory.ExpressionStatement(expression);
    }

    private static string PrefixGlobalIfNotPresent(string serviceType)
    {
        if (serviceType.StartsWith("global::"))
        {
            return serviceType;
        }
        return $"global::{serviceType}";
    }

    internal static ExpressionStatementSyntax CreateRegistrationSyntaxFromFactoryMethod(MethodFactoryRegistration registration)
    {
        var keyed = registration.ServiceName is null ? string.Empty : "Keyed";
        var methodName = $"Add{keyed}{registration.Lifetime}";
        return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("services"),
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(methodName))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(registration.ServiceType))))))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(registration.MethodClassName),
                                    SyntaxFactory.IdentifierName(registration.MethodName)))))))
            .NormalizeWhitespace();
    }

    internal static ExpressionStatementSyntax CreateRegistrationSyntaxFromCollectionMethod(MethodCollectionRegistration registration)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(registration.MethodClassName),
                    SyntaxFactory.IdentifierName(registration.MethodName)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName("services"))))))
        .NormalizeWhitespace();
    }

    internal static MethodCollectionRegistration CreateCollectionRegistration(IMethodSymbol methodSymbol)
    {
        var registration = new MethodCollectionRegistration
        {
            MethodClassName = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName = methodSymbol.Name
        };

        return registration;
    }
}
