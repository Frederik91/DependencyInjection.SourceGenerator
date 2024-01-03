using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace DependencyInjection.SourceGenerator.LightInject;
public class ClassAttributeReceiver : ISyntaxContextReceiver
{
    private string _expectedAttribute;
    public ClassAttributeReceiver(string expectedAttribute) => _expectedAttribute = expectedAttribute;

    public List<INamedTypeSymbol> Classes { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            return;

        if (!HasAttribute(classDeclarationSyntax))
            return;

        INamedTypeSymbol? classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
        if (classSymbol == null)
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