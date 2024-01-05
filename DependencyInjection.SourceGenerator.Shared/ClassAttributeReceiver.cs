using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using DependencyInjection.SourceGenerator.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.Shared;
public class ClassAttributeReceiver() : ISyntaxContextReceiver
{
    private static readonly string[] _attributes = [nameof(RegisterAttribute), nameof(DecorateAttribute) ];

    public List<INamedTypeSymbol> Classes { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            return;

        if (!HasAttribute(classDeclarationSyntax))
            return;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
        {
            return;
        }

        Classes.Add(classSymbol);
    }
    protected bool HasAttribute(ClassDeclarationSyntax classDeclarationSyntax)
    {
        foreach (var attributeList in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (attribute.Name is GenericNameSyntax genericNameSyntax)
                    name = genericNameSyntax.Identifier.ToString();

                if (_attributes.Contains(name) || _attributes.Contains(name + "Attribute"))
                    return true;
            }
        }
        return false;
    }
}