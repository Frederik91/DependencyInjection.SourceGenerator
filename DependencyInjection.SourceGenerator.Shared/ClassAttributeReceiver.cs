using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
public class ClassAttributeReceiver(params string[] expectedAttributes) : ISyntaxContextReceiver
{
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
                if (expectedAttributes.Contains(attribute.Name.ToString()) || expectedAttributes.Contains(attribute.Name.ToString() + "Attribute"))
                    return true;
            }
        }
        return false;
    }
}