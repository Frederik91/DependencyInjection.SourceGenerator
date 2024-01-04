using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class DecorationMapper
{
    internal static Decoration? CreateDecoration(INamedTypeSymbol type)
    {
        var attribute = type.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == nameof(DecorateAttribute) || x.AttributeClass?.Name == nameof(DecorateAttribute).Replace("Attribute", ""));

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

        var serviceTypeArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(DecorateAttribute.ServiceType));
        if (serviceTypeArgument.Value.Value is INamedTypeSymbol serviceType)
        {
            serviceTypeName = type.ToDisplayString(displayFormat);
        }

        if (serviceTypeName is null)
            return null;

        return new Decoration
        {
            DecoratorTypeName = type.ToDisplayString(displayFormat),
            DecoratedTypeName = serviceTypeName
        };
    }
}
