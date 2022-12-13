using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using DependencyInjection.SourceGenerator.Enums;
using DependencyInjection.SourceGenerator.Attributes;

namespace DependencyInjection.SourceGenerator;

[Generator]
public class DependencyInjectionRegistrationGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // Get first existing CompositionRoot class
        var compositionRoot = context.Compilation.SyntaxTrees
            .SelectMany(x => x.GetRoot().DescendantNodes())
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(x => x.Identifier.Text == "CompositionRoot");

        if (compositionRoot is not null && !IsPartial(compositionRoot))
        {
            var descriptor = new DiagnosticDescriptor("CW.1", "CompositionRoot not patial", "CompositionRoot must be partial", "LightInject", DiagnosticSeverity.Error, true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, compositionRoot.GetLocation()));
            return;
        }

        // If CompositionRoot class exists, get namespace, if not, use root namespace of project
        var @namespace = GetNamespace(context, compositionRoot);

        if (context.SyntaxContextReceiver is not ClassAttributeReceiver receiver)
        {
            return;
        }

        // Get all types with the "Inject" attribute
        var classesToRegister = receiver.Classes;

        var source = GenerateCompositionRoot(compositionRoot is not null, @namespace, classesToRegister);
        var sourceText = source.ToFullString();
        context.AddSource("CompositionRoot.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    private static string GetNamespace(GeneratorExecutionContext context, ClassDeclarationSyntax? compositionRoot)
    {
        if (compositionRoot?.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)        
            return namespaceDeclarationSyntax.Name.ToString();
        
        if (compositionRoot?.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
            return fileScopedNamespaceDeclarationSyntax.Name.ToString();

        var @namespace = context.Compilation.SyntaxTrees
        .SelectMany(x => x.GetRoot().DescendantNodes())
        .OfType<NamespaceDeclarationSyntax>()
        .Select(x => x.Name.ToString())
        .Min();

        if (@namespace is not null)
            return @namespace;

        @namespace = context.Compilation.SyntaxTrees
        .SelectMany(x => x.GetRoot().DescendantNodes())
        .OfType<FileScopedNamespaceDeclarationSyntax>()
        .Select(x => x.Name.ToString())
        .Min();

        if (@namespace is not null)
            return @namespace;

        throw new NotSupportedException("Unable to calculate namespace for CompositionRoot");
    }

    public static CompilationUnitSyntax GenerateCompositionRoot(bool userdefinedCompositionRoot, string @namespace, IEnumerable<INamedTypeSymbol> classesToRegister)
    {

        var modifiers = new List<SyntaxToken>{
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword) };

        if (userdefinedCompositionRoot)
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

        var classModifiers = SyntaxFactory.TokenList(modifiers.ToArray());

        var bodyMembers = new List<ExpressionStatementSyntax>();
        if (userdefinedCompositionRoot)
            bodyMembers.Add(CreateRegisterServicesCall());
        foreach (var type in classesToRegister)
        {
            var interfaceName = type.Interfaces.FirstOrDefault()?.ToDisplayString();
            if (interfaceName is null)
                continue;

            var attribute = type.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == nameof(RegisterAttribute) || x.AttributeClass?.Name == nameof(RegisterAttribute).Replace("Attribute", ""));
            if (attribute is null)
                throw new ArgumentNullException("RegisterAttribute not found");

            var namedArguments = attribute.NamedArguments;

            var lifetimeArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(RegisterAttribute.Lifetime));

            // Get the value of the property
            var lifetimeText = lifetimeArgument.Value.Value?.ToString() ?? new RegisterAttribute().Lifetime.ToString();

            if (lifetimeText is null)
                throw new ArgumentNullException("Lifetime not found");

            Enum.TryParse<Lifetime>(lifetimeText, out var lifetime);

            var serviceNameArgument = namedArguments.FirstOrDefault(arg => arg.Key == nameof(RegisterAttribute.ServiceName));

            // Get the value of the property
            var serviceName = serviceNameArgument.Value.Value?.ToString();

            bodyMembers.Add(CreateServiceRegistration(interfaceName, type.ToDisplayString(), lifetime, serviceName));
        }

        var body = SyntaxFactory.Block(bodyMembers.ToArray());

        return SyntaxFactory.CompilationUnit()
        .WithUsings(
            SyntaxFactory.SingletonList<UsingDirectiveSyntax>(
                SyntaxFactory.UsingDirective(
                    SyntaxFactory.IdentifierName("LightInject"))))
        .WithMembers(
            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(@namespace))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration("CompositionRoot")
                        .WithModifiers(classModifiers)
                        .WithBaseList(
                            SyntaxFactory.BaseList(
                                SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                    SyntaxFactory.SimpleBaseType(
                                        SyntaxFactory.IdentifierName("ICompositionRoot")))))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.MethodDeclaration(
                                    SyntaxFactory.PredefinedType(
                                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                    SyntaxFactory.Identifier("Compose"))
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier("serviceRegistry"))
                                            .WithType(
                                                SyntaxFactory.IdentifierName("IServiceRegistry")))))
                                .WithBody(body)))))))
        .NormalizeWhitespace();
    }

    private static ExpressionStatementSyntax CreateRegisterServicesCall()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("RegisterServices"))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName("serviceRegistry"))))));
    }

    private static ExpressionStatementSyntax CreateServiceRegistration(string service, string implementation, Lifetime lifetime, string? serviceName)
    {
        var lifetimeName = lifetime switch
        {
            Lifetime.Singleton => "PerContainerLifetime",
            Lifetime.Scoped => "PerScopeLifetime",
            Lifetime.Transient => "PerRequestLifeTime",
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                        new SyntaxNodeOrToken[]{
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.ObjectCreationExpression(
                                                            SyntaxFactory.IdentifierName("PerRequestLifetime"))
                                                        .WithArgumentList(
                                                            SyntaxFactory.ArgumentList())),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal("Test")))});

        var lifetimeSyntax = SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName(lifetimeName))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList()));

        var args = new List<SyntaxNodeOrToken>();
        if (!string.IsNullOrEmpty(serviceName))
        {
            var serviceNameSyntax = SyntaxFactory.Argument(
                                                       SyntaxFactory.LiteralExpression(
                                                           SyntaxKind.StringLiteralExpression,
                                                           SyntaxFactory.Literal(serviceName!)));
            args.Add(serviceNameSyntax);
            args.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
        }
        args.Add(lifetimeSyntax);

        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(args));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("serviceRegistry"),
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Register"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList<TypeSyntax>(
                                new SyntaxNodeOrToken[]{
                            SyntaxFactory.IdentifierName(service),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.IdentifierName(implementation)})))))
             .WithArgumentList(argumentList));
    }

    private bool IsPartial(ClassDeclarationSyntax compositionRoot)
    {
        return compositionRoot.Modifiers.Any(x => x.Text == "partial");
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassAttributeReceiver(nameof(RegisterAttribute)));
    }
}
