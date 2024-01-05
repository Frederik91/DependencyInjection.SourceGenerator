using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationMapper
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
}
