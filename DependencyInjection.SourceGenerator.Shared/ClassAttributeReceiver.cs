using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using DependencyInjection.SourceGenerator.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.Shared;
public class ClassAttributeReceiver : ISyntaxContextReceiver
{
    private static readonly string[] _classAttributes = [nameof(RegisterAttribute), nameof(DecorateAttribute)];
    private static readonly string[] _methodAttributes = [nameof(RegistrationExtensionAttribute)];

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

                if (_classAttributes.Contains(name) || _classAttributes.Contains(name + "Attribute"))
                    return true;
            }
        }

        foreach (var member in classDeclarationSyntax.Members)
        {
            if (member is not MethodDeclarationSyntax methodDeclarationSyntax)
                continue;

            foreach (var attributeList in methodDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (attribute.Name is GenericNameSyntax genericNameSyntax)
                        name = genericNameSyntax.Identifier.ToString();

                    if (_methodAttributes.Contains(name) || _methodAttributes.Contains(name + "Attribute"))
                        return true;
                }
            }
        }

        return false;
    }
}