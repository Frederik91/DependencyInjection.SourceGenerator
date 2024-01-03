using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DependencyInjection.SourceGenerator.Shared;
public class ClassAttributeReceiver(string expectedAttribute) : ISyntaxContextReceiver
{
    private readonly string _expectedAttribute = expectedAttribute;

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
                if (attribute.Name.ToString() == _expectedAttribute || attribute.Name.ToString() + "Attribute" == _expectedAttribute)
                    return true;
            }
        }
        return false;
    }
}