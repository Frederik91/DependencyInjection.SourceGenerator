using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationMapper
{
    internal static List<Registration> CreateRegistration(INamedTypeSymbol type)
    {
        var attributes = TypeHelper.GetClassAttributes<RegisterAttribute>(type);

        if (!attributes.Any())
            return [];

        var result = new List<Registration>();
        foreach (var attribute in attributes)
        {

            var serviceType = TypeHelper.GetServiceType(type, attribute);
            if (serviceType is null)
                continue;

            var lifetimeValue = TypeHelper.GetAttributeValue(attribute, nameof(RegisterAttribute.Lifetime));
            if (!Enum.TryParse<Lifetime>(lifetimeValue?.ToString(), out var lifetime))
                lifetime = new RegisterAttribute().Lifetime;

            var serviceNameArgument = TypeHelper.GetAttributeValue(attribute, nameof(RegisterAttribute.ServiceName));

            // Get the value of the property
            var serviceName = serviceNameArgument?.ToString();

            var implementationTypeName = TypeHelper.GetFullName(type);

            if (TypeHelper.IsSameType(type, serviceType.Type))
            {
                implementationTypeName = serviceType.Name;
                serviceType = null;
            }

            var registration = new Registration
            {
                ImplementationTypeName = implementationTypeName,
                Lifetime = lifetime,
                ServiceName = serviceName,
                ServiceType = serviceType?.Name
            };
            result.Add(registration);
        }

        return result;
    }
}
