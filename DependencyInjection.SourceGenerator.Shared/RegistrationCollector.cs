using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationCollector
{
    internal static List<INamedTypeSymbol> GetTypes(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ClassAttributeReceiver receiver)
        {
            return [];
        }

        return receiver.Classes;
    }


    internal static List<Registration> GetRegisterAllTypes(GeneratorExecutionContext context)
    {
        var registerAllAttributes = TypeHelper.GetAttributes<RegisterAllAttribute>(context.Compilation.Assembly.GetAttributes());

        var result = new List<Registration>();
        foreach (var registerAllAttribute in registerAllAttributes)
        {
            var serviceType = TypeHelper.GetServiceTypeFromAttribute(registerAllAttribute);
            if (serviceType is null)
                continue;

            var lifetime = TypeHelper.GetAttributeValue(registerAllAttribute, nameof(RegisterAllAttribute.Lifetime));
            if (!Enum.TryParse<Lifetime>(lifetime?.ToString(), out var lifetimeValue))
                lifetimeValue = Lifetime.Transient;

            var includeServiceName = TypeHelper.GetAttributeValue(registerAllAttribute, nameof(RegisterAllAttribute.IncludeServiceName));
            if (!bool.TryParse(includeServiceName?.ToString(), out var includeServiceNameValue))
                includeServiceNameValue = false;

            var implementedTypes = GetImplementedTypes(serviceType, context);
            foreach (var implementedType in implementedTypes)
            {
                var registration = new Registration
                {
                    ImplementationTypeName = implementedType.ToDisplayString(TypeHelper.DisplayFormat),
                    Lifetime = lifetimeValue,
                    ServiceName = includeServiceNameValue ? implementedType.Name : null,
                    ServiceType = serviceType.ToDisplayString(TypeHelper.DisplayFormat)
                };
                result.Add(registration);
            }
        }

        return result;
    }

    private static List<INamedTypeSymbol> GetImplementedTypes(INamedTypeSymbol? serviceType, GeneratorExecutionContext context)
    {
        var result = new List<INamedTypeSymbol>();
        if (serviceType is null)
            return result;

        var compilation = context.Compilation;
        foreach (var typeName in compilation.Assembly.TypeNames)
        {
            var types = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type).OfType<INamedTypeSymbol>();
            foreach (var type in types)
            {
                if (type.AllInterfaces.Any(i => i.ToDisplayString(TypeHelper.DisplayFormat) == serviceType.ToDisplayString(TypeHelper.DisplayFormat)))
                    result.Add(type);
                else if (BaseTypeIsType(type, serviceType))
                    result.Add(type);
            }

        }

        return result;

    }

    private static bool BaseTypeIsType(INamedTypeSymbol type, INamedTypeSymbol typeToCheck)
    {
        if (type.BaseType is not { } baseType)
            return false;

        if (baseType.ToDisplayString(TypeHelper.DisplayFormat) == typeToCheck.ToDisplayString(TypeHelper.DisplayFormat))
            return true;

        return BaseTypeIsType(baseType, typeToCheck);
    }
}
