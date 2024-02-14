using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using DependencyInjection.SourceGenerator.Contracts.Attributes;
using System;

namespace DependencyInjection.SourceGenerator.Shared;
public class ClassAttributeReceiver : ISyntaxContextReceiver
{
    private readonly string[] _classAttributes = [nameof(RegisterAttribute), nameof(DecorateAttribute)];
    private readonly string[] _methodAttributes = [];
    private readonly string[] _assemblyAttributes = [nameof(RegisterAllAttribute)];

    public List<INamedTypeSymbol> Types { get; } = [];

    public ClassAttributeReceiver(string[]? additionalAssemblyAttributes = null, string[]? additionalClassAttributes = null, string[]? additionalMethodAttributes = null)
    {
        if (additionalAssemblyAttributes is not null)
            _assemblyAttributes = [.. _assemblyAttributes, .. additionalAssemblyAttributes];

        if (additionalClassAttributes is not null)
            _classAttributes = [.. _classAttributes, .. additionalClassAttributes];

        if (additionalMethodAttributes is not null)
            _methodAttributes = [.. _methodAttributes, .. additionalMethodAttributes];
    }


    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is TypeDeclarationSyntax typeDeclarationSyntax)
            EvaluateClass(context, typeDeclarationSyntax);
    }

    private void EvaluateClass(GeneratorSyntaxContext context, TypeDeclarationSyntax typeDeclarationSyntax)
    {
        if (!HasAttribute(typeDeclarationSyntax))
            return;

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        Types.Add(typeSymbol);
    }

    protected bool HasAttribute(TypeDeclarationSyntax classDeclarationSyntax)
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (HasAttribute(attributeLists, _classAttributes))
            return true;

        foreach (var member in classDeclarationSyntax.Members)
        {
            if (member is not MethodDeclarationSyntax methodDeclarationSyntax)
                continue;

            if (HasAttribute(methodDeclarationSyntax.AttributeLists, _methodAttributes))
                return true;
        }

        return false;
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string[] attributes)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (attribute.Name is GenericNameSyntax genericNameSyntax)
                    name = genericNameSyntax.Identifier.ToString();

                if (attributes.Contains(name) || attributes.Contains(name + "Attribute"))
                    return true;
            }
        }
        return false;
    }
}