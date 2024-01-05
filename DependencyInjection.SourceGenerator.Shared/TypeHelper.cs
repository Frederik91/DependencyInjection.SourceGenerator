using DependencyInjection.SourceGenerator.Contracts.Attributes;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class TypeHelper
{
    internal static SymbolDisplayFormat DisplayFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat;


    internal static AttributeData? GetAttribute<TAttribute>(INamedTypeSymbol type) where TAttribute : Attribute
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is null)
                continue;

            var name = attribute.AttributeClass.Name;
            if (!name.EndsWith("Attribute"))
                name += "Attribute";

            if (name == typeof(TAttribute).Name)
                return attribute;
        }
        return null;
    }

    internal static object? GetAttributeValue(AttributeData attribute, string key)
    {
        var namedArguments = attribute.NamedArguments;
        var argument = namedArguments.FirstOrDefault(arg => arg.Key == key);
        return argument.Value.Value;
    }

    internal static INamedTypeSymbol? GetServiceTypeFromAttribute(AttributeData attribute)
    {
        var serviceTypeArgument = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "ServiceType");
        if (!serviceTypeArgument.Value.IsNull)
            return serviceTypeArgument.Value.Value as INamedTypeSymbol;

        if (attribute.AttributeClass?.ConstructedFrom is INamedTypeSymbol attributeClass && attributeClass.IsGenericType && attribute.AttributeClass?.TypeArguments.Length > 0)
            return attribute.AttributeClass?.TypeArguments[0] as INamedTypeSymbol;

        return default;
    }

    internal static bool IsSameType(INamedTypeSymbol type1, INamedTypeSymbol type2)
    {
        if (type1.ToDisplayString(DisplayFormat) == type2.ToDisplayString(DisplayFormat))
            return true;

        return type1.IsGenericType
            && type2.IsGenericType
            && type1.ConstructUnboundGenericType().ToDisplayString(DisplayFormat) == type2.ConstructUnboundGenericType().ToDisplayString(DisplayFormat);
    }

    internal static ServiceType GetServiceType(INamedTypeSymbol type, AttributeData attribute)
    {
        var serviceType = GetServiceTypeFromAttribute(attribute);
        if (serviceType is not null)
        {
            return new(serviceType, serviceType.ToDisplayString(DisplayFormat));
        }

        var interfaceType = type.Interfaces.FirstOrDefault();

        if (interfaceType is null)
        {
            if (type.BaseType is null || type.BaseType.Kind != SymbolKind.ErrorType)
            {
                return new(type, type.ToDisplayString(DisplayFormat));
            }
            return new(type, type.ContainingNamespace.ToDisplayString(DisplayFormat) + "." + type.BaseType.Name);
        }

        var serviceTypeName = interfaceType.ToDisplayString(DisplayFormat);
        if (interfaceType.Kind == SymbolKind.ErrorType && !string.IsNullOrEmpty(serviceTypeName))
        {
            return new(type, type.ContainingNamespace.ToDisplayString(DisplayFormat) + "." + serviceTypeName);
        }

        return new(interfaceType, serviceTypeName);
    }
}

public record ServiceType(INamedTypeSymbol Type, string Name);