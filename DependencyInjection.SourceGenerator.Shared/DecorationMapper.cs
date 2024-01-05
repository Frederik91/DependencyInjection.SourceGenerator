using DependencyInjection.SourceGenerator.Contracts.Attributes;
using Microsoft.CodeAnalysis;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class DecorationMapper
{
    internal static Decoration? CreateDecoration(INamedTypeSymbol type)
    {
        var attribute = TypeHelper.GetAttribute<DecorateAttribute>(type);

        if (attribute is null)
            return null;

        var serviceTypeName = TypeHelper.GetServiceType(type, attribute);
        if (serviceTypeName is null)
            return null;

        return new Decoration
        {
            DecoratorTypeName = type.ToDisplayString(TypeHelper.DisplayFormat),
            DecoratedTypeName = serviceTypeName.Name
        };
    }
}
