using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationMapper
{
    internal static Registration? CreateRegistration(INamedTypeSymbol type)
    {
        var attribute = type.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == nameof(RegisterAttribute) || x.AttributeClass?.Name == nameof(RegisterAttribute).Replace("Attribute", ""));

        if (attribute is null)
            return null;

        var displayFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        var @interface = type.Interfaces.FirstOrDefault();
        var serviceTypeName = @interface?.ToDisplayString(displayFormat);
        if (@interface is null)
        {
            if (type.BaseType is null || type.BaseType.Kind != SymbolKind.ErrorType)
                serviceTypeName = type.ToDisplayString(displayFormat);
            else
                serviceTypeName = type.ContainingNamespace.ToDisplayString(displayFormat) + "." + type.BaseType.Name;
        }
        else if (@interface.Kind == SymbolKind.ErrorType && !string.IsNullOrEmpty(serviceTypeName))
            serviceTypeName = type.ContainingNamespace.ToDisplayString(displayFormat) + "." + serviceTypeName;



        var namedArguments = attribute.NamedArguments;

        var serviceTypeArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(RegisterAttribute.ServiceType));
        if (serviceTypeArgument.Value.Value is INamedTypeSymbol serviceType)
        {
            serviceTypeName = type.ToDisplayString(displayFormat);
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
            ImplementationTypeName = type.ToDisplayString(displayFormat),
            Lifetime = lifetime,
            ServiceName = serviceName,
            ServiceType = serviceTypeName
        };
    }
}
