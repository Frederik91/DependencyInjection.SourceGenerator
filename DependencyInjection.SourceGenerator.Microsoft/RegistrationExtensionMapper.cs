using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationExtensionMapper
{
    internal static Registration? CreateRegistration(INamedTypeSymbol type)
    {
        var attributes = TypeHelper.GetClassAttributes<RegisterAttribute>(type).FirstOrDefault();

        if (attributes is null)
            return null;

        var serviceType = TypeHelper.GetServiceType(type, attributes);
        if (serviceType is null)
            return null;

        var lifetimeValue = TypeHelper.GetAttributeValue(attributes, nameof(RegisterAttribute.Lifetime));
        if (!Enum.TryParse<Lifetime>(lifetimeValue?.ToString(), out var lifetime))
            lifetime = new RegisterAttribute().Lifetime;

        var serviceNameArgument = TypeHelper.GetAttributeValue(attributes, nameof(RegisterAttribute.ServiceName));

        // Get the value of the property
        var serviceName = serviceNameArgument?.ToString();

        var implementationTypeName = TypeHelper.GetFullName(type);

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
}

public record RegistrationExtension(string ClassFullName, string MethodName, List<Diagnostic> Errors);