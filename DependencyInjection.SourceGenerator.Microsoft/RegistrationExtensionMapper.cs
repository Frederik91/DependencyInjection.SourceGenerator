using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationExtensionMapper
{
    internal static Registration? CreateRegistration(INamedTypeSymbol type)
    {
        var attribute = TypeHelper.GetClassAttribute<RegisterAttribute>(type);

        if (attribute is null)
            return null;

        var serviceType = TypeHelper.GetServiceType(type, attribute);
        if (serviceType is null)
            return null;

        var lifetimeValue = TypeHelper.GetAttributeValue(attribute, nameof(RegisterAttribute.Lifetime));
        if (!Enum.TryParse<Lifetime>(lifetimeValue?.ToString(), out var lifetime))
            lifetime = new RegisterAttribute().Lifetime;

        var serviceNameArgument = TypeHelper.GetAttributeValue(attribute, nameof(RegisterAttribute.ServiceName));

        // Get the value of the property
        var serviceName = serviceNameArgument?.ToString();

        var implementationTypeName = type.ToDisplayString(TypeHelper.DisplayFormat);

        if (TypeHelper.IsSameType(type, serviceType.Type))
        {
            implementationTypeName = serviceType.Name;
            serviceType = null;
        }

        return new Registration
        {
            ImplementationTypeName = implementationTypeName,
            Lifetime = lifetime,
            ServiceName = serviceName,
            ServiceType = serviceType?.Name
        };
    }

    internal static List<RegistrationExtension> CreateRegistrationExtensions(INamedTypeSymbol type)
    {
        var registrations = new List<RegistrationExtension>();
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method)
                continue;

            var attribute = TypeHelper.GetAttribute<RegistrationExtensionAttribute>(member.GetAttributes());
            if (attribute is null)
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

            if (method.Parameters.FirstOrDefault()?.Type.ToDisplayString(TypeHelper.DisplayFormat) != "global::Microsoft.Extensions.DependencyInjection.IServiceCollection")
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

            var registration = new RegistrationExtension(type.ToDisplayString(TypeHelper.DisplayFormat), method.Name, errors);
            registrations.Add(registration);
        }
        return registrations;
    }
}

public record RegistrationExtension(string ClassFullName, string MethodName, List<Diagnostic> Errors);