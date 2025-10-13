using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DependencyInjection.SourceGenerator.Microsoft.Helpers;

internal static class FactoryMapper
{
    internal static IReadOnlyList<FactoryParameter>? GetFactoryParameters(INamedTypeSymbol type)
    {
        FactoryParameter[]? bestMatch = null;

        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.IsStatic)
            {
                continue;
            }

            if (constructor.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
            {
                continue;
            }

            var parameters = new List<FactoryParameter>();
            foreach (var parameter in constructor.Parameters)
            {
                if (!HasFactoryArgument(parameter))
                {
                    continue;
                }

                parameters.Add(new FactoryParameter(
                    TypeHelper.GetFullName(parameter.Type),
                    parameter.Name));
            }

            if (parameters.Count == 0)
            {
                continue;
            }

            if (bestMatch is null || parameters.Count > bestMatch.Length)
            {
                bestMatch = parameters.ToArray();
            }
        }

        return bestMatch;
    }

    internal static FactoryRegistration? CreateFactoryRegistration(ClassRegistration registration, IReadOnlyList<FactoryParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return null;
        }

        var implementationSymbol = registration.ImplementationTypeSymbol;
        var serviceSymbol = registration.ServiceTypeSymbol ?? registration.ImplementationTypeSymbol;

        var namespaceSymbol = implementationSymbol.ContainingNamespace;
        var namespaceName = namespaceSymbol.IsGlobalNamespace ? string.Empty : namespaceSymbol.ToDisplayString();

        var baseName = CreateBaseName(implementationSymbol);
        var interfaceName = EnsureInterfacePrefix(baseName) + "Factory";
        var className = TrimInterfacePrefix(baseName) + "Factory";
        return new FactoryRegistration
        {
            Namespace = namespaceName,
            InterfaceName = interfaceName,
            ClassName = className,
            ServiceTypeName = PrefixGlobal(registration.ServiceType ?? registration.ImplementationTypeName),
            ImplementationTypeName = PrefixGlobal(registration.ImplementationTypeName),
            Parameters = parameters,
            ImplementationTypeSymbol = implementationSymbol,
            ServiceTypeSymbol = serviceSymbol
        };
    }

    internal static ExpressionStatementSyntax CreateFactoryRegistrationExpression(FactoryRegistration registration)
    {
        var interfaceType = PrefixGlobal(CombineNamespace(registration.Namespace, registration.InterfaceName));
        var classType = PrefixGlobal(CombineNamespace(registration.Namespace, registration.ClassName));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("services"),
                    SyntaxFactory.GenericName("AddScoped")
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SeparatedList<TypeSyntax>(
                                    new SyntaxNodeOrToken[]
                                    {
                                        SyntaxFactory.IdentifierName(interfaceType),
                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                        SyntaxFactory.IdentifierName(classType)
                                    }))))));
    }

    internal static CompilationUnitSyntax CreateFactoryCompilationUnit(FactoryRegistration registration, string generatorVersion)
    {
        var members = CreateFactoryMembers(registration, generatorVersion);

        if (string.IsNullOrWhiteSpace(registration.Namespace))
        {
            return SyntaxFactory.CompilationUnit()
                .WithLeadingTrivia(Trivia.CreateHeaderTrivia())
                .AddMembers(members)
                .NormalizeWhitespace();
        }

        var namespaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(registration.Namespace))
            .WithNamespaceKeyword(Trivia.CreateTrivia())
            .AddMembers(members);

        return SyntaxFactory.CompilationUnit()
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();
    }

    internal static string CreateFactoryHintName(FactoryRegistration registration)
    {
        var namespacePart = string.IsNullOrWhiteSpace(registration.Namespace)
            ? "Global"
            : registration.Namespace.Replace('.', '_');

        return $"{namespacePart}_{registration.ClassName}.g.cs";
    }

    private static MemberDeclarationSyntax[] CreateFactoryMembers(FactoryRegistration registration, string generatorVersion)
    {
        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(registration.InterfaceName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithMembers(
                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                    SyntaxFactory.MethodDeclaration(
                            SyntaxFactory.ParseTypeName(registration.ServiceTypeName),
                            SyntaxFactory.Identifier("Create"))
                        .WithParameterList(CreateParameterList(registration.Parameters))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));

        var createFactoryInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("global::Microsoft.Extensions.DependencyInjection.ActivatorUtilities"),
                    SyntaxFactory.IdentifierName("CreateFactory")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.TypeOfExpression(
                                    SyntaxFactory.ParseTypeName(registration.ImplementationTypeName))),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.Argument(CreateFactoryParameterTypesArray(registration.Parameters))
                        })));

        var objectFactoryVariable = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("s_objectFactory"))
            .WithInitializer(SyntaxFactory.EqualsValueClause(createFactoryInvocation));

        var objectFactoryField = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName("global::Microsoft.Extensions.DependencyInjection.ObjectFactory"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(objectFactoryVariable)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

        var serviceProviderField = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName("global::System.IServiceProvider"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("_serviceProvider")))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

        var constructor = SyntaxFactory.ConstructorDeclaration(registration.ClassName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("serviceProvider"))
                            .WithType(SyntaxFactory.ParseTypeName("global::System.IServiceProvider")))))
            .WithBody(
                SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName("_serviceProvider"),
                            SyntaxFactory.IdentifierName("serviceProvider")))));

        var createMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName(registration.ServiceTypeName),
                SyntaxFactory.Identifier("Create"))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(CreateParameterList(registration.Parameters))
            .WithBody(
                SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(registration.ServiceTypeName),
                            SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.IdentifierName("s_objectFactory"))
                                .WithArgumentList(CreateObjectFactoryArgumentList(registration.Parameters))))));

        var classDeclaration = SyntaxFactory.ClassDeclaration(registration.ClassName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithAttributeLists(CreateClassAttributes(generatorVersion))
            .WithBaseList(
                SyntaxFactory.BaseList(
                    SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(registration.InterfaceName)))))
            .WithMembers(SyntaxFactory.List(new MemberDeclarationSyntax[]
            {
                objectFactoryField,
                serviceProviderField,
                constructor,
                createMethod
            }));

        return new MemberDeclarationSyntax[] { interfaceDeclaration, classDeclaration };
    }

    private static ArgumentListSyntax CreateObjectFactoryArgumentList(IReadOnlyList<FactoryParameter> parameters)
    {
        var arguments = new List<SyntaxNodeOrToken>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("_serviceProvider")),
            SyntaxFactory.Token(SyntaxKind.CommaToken),
            SyntaxFactory.Argument(CreateRuntimeArgumentsArray(parameters))
        };

        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments));
    }

    private static ExpressionSyntax CreateRuntimeArgumentsArray(IReadOnlyList<FactoryParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return SyntaxFactory.ParseExpression("global::System.Array.Empty<object>()");
        }

        var builder = new StringBuilder();
        builder.Append("new object[] {");

        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(parameters[i].Name);
        }

        builder.Append(" }");

        return SyntaxFactory.ParseExpression(builder.ToString());
    }

    private static ExpressionSyntax CreateFactoryParameterTypesArray(IReadOnlyList<FactoryParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return SyntaxFactory.ParseExpression("global::System.Type.EmptyTypes");
        }

        var arrayType = SyntaxFactory.ArrayType(
                SyntaxFactory.ParseTypeName("global::System.Type"))
            .WithRankSpecifiers(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression()))));

        var nodes = new List<SyntaxNodeOrToken>();
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                nodes.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            nodes.Add(
                SyntaxFactory.TypeOfExpression(
                    SyntaxFactory.ParseTypeName(parameters[i].TypeName)));
        }

        var initializer = SyntaxFactory.InitializerExpression(
            SyntaxKind.ArrayInitializerExpression,
            SyntaxFactory.SeparatedList<ExpressionSyntax>(nodes));

        return SyntaxFactory.ArrayCreationExpression(arrayType)
            .WithInitializer(initializer);
    }


    private static ParameterListSyntax CreateParameterList(IReadOnlyList<FactoryParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return SyntaxFactory.ParameterList();
        }

        var nodes = new List<SyntaxNodeOrToken>();
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                nodes.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            var parameter = parameters[i];
            nodes.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name))
                    .WithType(SyntaxFactory.ParseTypeName(parameter.TypeName)));
        }

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(nodes));
    }

    private static SyntaxList<AttributeListSyntax> CreateClassAttributes(string generatorVersion)
    {
        var generatedCode = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName("global::System.CodeDom.Compiler.GeneratedCode"))
                    .WithArgumentList(
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal("DependencyInjection.SourceGenerator.Microsoft"))),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(generatorVersion)))
                                })))));

        var excludeFromCodeCoverage = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage"))));

        return SyntaxFactory.List(new[] { generatedCode, excludeFromCodeCoverage });
    }

    private static bool HasFactoryArgument(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass is null)
            {
                continue;
            }

            if (attribute.AttributeClass.Name == nameof(global::Microsoft.Extensions.DependencyInjection.FactoryArgumentAttribute) &&
                attribute.AttributeClass.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.DependencyInjection")
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateBaseName(INamedTypeSymbol symbol)
    {
        var baseName = TrimGenericSuffix(symbol.Name);
        if (symbol.IsGenericType && symbol.TypeArguments.Length > 0)
        {
            var builder = new StringBuilder(baseName);
            builder.Append("Of");
            builder.Append(string.Join("And", symbol.TypeArguments.Select(GetTypeArgumentName)));
            return builder.ToString();
        }

        return baseName;
    }

    private static string GetTypeArgumentName(ITypeSymbol symbol)
    {
        return symbol switch
        {
            INamedTypeSymbol namedTypeSymbol => CreateBaseName(namedTypeSymbol),
            IArrayTypeSymbol arrayTypeSymbol => GetTypeArgumentName(arrayTypeSymbol.ElementType) + "Array",
            IPointerTypeSymbol pointerTypeSymbol => GetTypeArgumentName(pointerTypeSymbol.PointedAtType) + "Pointer",
            _ => TrimGenericSuffix(symbol.Name)
        };
    }

    private static string TrimGenericSuffix(string name)
    {
        var index = name.IndexOf('`');
        return index >= 0 ? name[..index] : name;
    }

    private static string EnsureInterfacePrefix(string name)
    {
        if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
        {
            return name;
        }

        return "I" + name;
    }

    private static string TrimInterfacePrefix(string name)
    {
        if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
        {
            return name[1..];
        }

        return name;
    }

    private static string CombineNamespace(string namespaceName, string typeName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return typeName;
        }

        return $"{namespaceName}.{typeName}";
    }

    private static string PrefixGlobal(string typeName)
    {
        return typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName : $"global::{typeName}";
    }
}
