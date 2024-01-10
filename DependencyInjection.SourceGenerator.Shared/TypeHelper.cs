using DependencyInjection.SourceGenerator.Contracts.Attributes;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class TypeHelper
{
    private static readonly SymbolDisplayFormat _displayFormat = SymbolDisplayFormat.FullyQualifiedFormat;


    internal static List<AttributeData> GetClassAttributes<TAttribute>(INamedTypeSymbol type) where TAttribute : Attribute
    {
        var attributes = type.GetAttributes();
        return GetAttributes<TAttribute>(attributes);
    }

    internal static List<AttributeData> GetAttributes<TAttribute>(ImmutableArray<AttributeData> attributes) where TAttribute : Attribute
    {
        var result = new List<AttributeData>();
        var fullName = "global::" + typeof(TAttribute).FullName;
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass is null)
                continue;
            
            var attributeName = "global::" + attribute.AttributeClass.ContainingNamespace + "." + attribute.AttributeClass.Name;

            if (fullName == attributeName)
                result.Add(attribute);
        }
        return result;
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

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol argumentServiceType)
            return argumentServiceType;

        return default;
    }

    internal static bool IsSameType(INamedTypeSymbol type1, INamedTypeSymbol type2)
    {
        if (GetFullName(type1) == GetFullName(type2, type1.ContainingNamespace))
            return true;

        return type1.IsGenericType
            && type2.IsGenericType
            && GetFullName(type1.ConstructUnboundGenericType()) == GetFullName(type2.ConstructUnboundGenericType(), type1.ContainingNamespace);
    }

    internal static ServiceType GetServiceType(INamedTypeSymbol type, AttributeData attribute)
    {
        var serviceType = GetServiceTypeFromAttribute(attribute);
        if (serviceType is not null)
        {
            var serviceTypeName = GetFullName(serviceType, type.ContainingNamespace);
            return new(serviceType, serviceTypeName);
        }

        var interfaceType = type.Interfaces.FirstOrDefault();

        if (interfaceType is null)
        {           
            
            if (type.BaseType is null || IsSystemObject(type.BaseType))
            {
                return new(type, GetFullName(type));
            }
            var baseTypeName = GetFullName(type.BaseType, type.ContainingNamespace);
            return new(type.BaseType, baseTypeName);
        }

        var interfaceTypeName = GetFullName(interfaceType, type.ContainingNamespace);
        return new(interfaceType, interfaceTypeName);
    }

    private static bool IsSystemObject(INamedTypeSymbol type)
    {
        return type.Name == "Object" && type.ContainingNamespace.Name == "System";        
    }

    internal static string GetFullName(ITypeSymbol type, INamespaceSymbol? fallbackNamespace = null)
    {
        if (type.Kind != SymbolKind.ErrorType)
            return type.ToDisplayString(_displayFormat);

        if (fallbackNamespace is null)
            return type.Name;

        return $"{fallbackNamespace.ToDisplayString(_displayFormat)}.{type.Name}";
    }
}

public record ServiceType(INamedTypeSymbol Type, string Name);