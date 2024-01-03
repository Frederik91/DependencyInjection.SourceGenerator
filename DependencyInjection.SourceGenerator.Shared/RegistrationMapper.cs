using DependencyInjection.SourceGenerator.Shared.Attributes;
using DependencyInjection.SourceGenerator.Shared.Enums;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationMapper
{
    internal static Registration? CreateRegistration(INamedTypeSymbol type)
    {
        var @interface = type.Interfaces.FirstOrDefault();
        var serviceTypeName = @interface?.ToDisplayString();
        if (@interface is null)
        {
            if (type.BaseType is null || type.BaseType.Kind != SymbolKind.ErrorType)
                serviceTypeName = type.ToDisplayString();
            else
                serviceTypeName = type.ContainingNamespace.ToDisplayString() + "." + type.BaseType.Name;
        }
        else if (@interface.Kind == SymbolKind.ErrorType && !string.IsNullOrEmpty(serviceTypeName))
            serviceTypeName = type.ContainingNamespace.ToDisplayString() + "." + serviceTypeName;

        var attribute = type.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == nameof(RegisterAttribute) || x.AttributeClass?.Name == nameof(RegisterAttribute).Replace("Attribute", ""));
        if (attribute is null)
            return null;

        var namedArguments = attribute.NamedArguments;

        var serviceTypeArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(RegisterAttribute.ServiceType));
        if (serviceTypeArgument.Value.Value is INamedTypeSymbol serviceType)
        {
            serviceTypeName = type.ToDisplayString();
        }

        if (serviceTypeName is null)
            return null;

        var lifetimeArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(RegisterAttribute.Lifetime));

        // Get the value of the property
        var lifetimeText = lifetimeArgument.Value.Value?.ToString() ?? new RegisterAttribute().Lifetime.ToString();

        if (lifetimeText is null)
            return null;

        Enum.TryParse<Lifetime>(lifetimeText, out var lifetime);

        var serviceNameArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(RegisterAttribute.ServiceName));

        // Get the value of the property
        var serviceName = serviceNameArgument.Value.Value?.ToString();

        return new Registration
        {
            ImplementationTypeName = type.ToDisplayString(),
            Lifetime = lifetime,
            ServiceName = serviceName,
            ServiceType = serviceTypeName
        };
    }
}
