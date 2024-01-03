using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using VerifyCS = DependencyInjection.SourceGenerator.LightInject.Tests.CSharpSourceGeneratorVerifier<DependencyInjection.SourceGenerator.LightInject.DependencyInjectionRegistrationGenerator>;
using Microsoft.CodeAnalysis.Testing;
using System.ComponentModel.Design;

namespace DependencyInjection.SourceGenerator.LightInject.Tests;

public class DependencyInjectionRegistrationGeneratorTests
{
    private static readonly string _header = """
        // <auto-generated/>
        #pragma warning disable
        #nullable enable
        using LightInject;
        
        namespace DependencyInjection.SourceGenerator.Demo;
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        
        """;


    private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("DependencyInjection.SourceGenerator.Demo",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(DependencyInjectionRegistrationGeneratorTests).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));

    private readonly ImmutableArray<string> references = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(assembly => !assembly.IsDynamic)
    .Select(assembly => assembly.Location)
    .ToImmutableArray();

    private async Task RunTestAsync(string code, string expectedResult)
    {
        var tester = new VerifyCS.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(DependencyInjectionRegistrationGenerator), "CompositionRoot.g.cs",
                            SourceText.From(expectedResult, Encoding.UTF8))
                    }
                },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        };

        tester.ReferenceAssemblies.AddAssemblies(references);
        tester.TestState.AdditionalReferences.Add(typeof(GenerateAutomaticInterfaceAttribute).Assembly);
        tester.TestState.AdditionalReferences.Add(typeof(Contracts.Attributes.RegisterAttribute).Assembly);
        tester.TestState.AdditionalReferences.Add(typeof(global::LightInject.IServiceContainer).Assembly);
        
        await tester.RunAsync();
    }

    [Fact]
    public async Task CreateCompositionRoot_RegisterService_NoExistingCompositionRoot()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Register]
public class Service : IService {}
public interface IService {}

""";

<<<<<<< Updated upstream
        var expected = _header + """
=======
        var expected = """
using LightInject;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;
>>>>>>> Stashed changes
public class CompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<DependencyInjection.SourceGenerator.Demo.IService, DependencyInjection.SourceGenerator.Demo.Service>(new PerRequestLifeTime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }

    [Fact]
    public async Task CreateCompositionRoot_RegisterService_ExistingCompositionRoot()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;
using LightInject;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Register]
public class Service : IService {}
public interface IService {}

public partial class CompositionRoot : ICompositionRoot
{
    public static void RegisterServices(IServiceRegistry serviceRegistry)
    {
        
    } 
}

""";

<<<<<<< Updated upstream
        var expected = _header + """
=======
        var expected = """
using LightInject;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;
>>>>>>> Stashed changes
public partial class CompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        RegisterServices(serviceRegistry);
        serviceRegistry.Register<DependencyInjection.SourceGenerator.Demo.IService, DependencyInjection.SourceGenerator.Demo.Service>(new PerRequestLifeTime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }

    [Fact]
    public async Task Register_SpecifiedLifetime_And_ServiceName()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Register(Lifetime = Lifetime.Scoped, ServiceName = "Test")]
public class Service : IService {}
public interface IService {}

""";

<<<<<<< Updated upstream
        var expected = _header + """
=======
        var expected = """
using LightInject;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;
>>>>>>> Stashed changes
public class CompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<DependencyInjection.SourceGenerator.Demo.IService, DependencyInjection.SourceGenerator.Demo.Service>("Test", new PerScopeLifetime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }

    [Fact]
    public async Task CreateCompositionRoot_RegisterService_AutomaticlyGeneratedInterface()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.Tests;
[GenerateAutomaticInterface]
[Register]
public class AutomaticlyGeneratedService : IAutomaticlyGeneratedService
{
    public void DoSomething()
    {
    }
}
""";

        var expected = _header.Replace("Demo", "Tests") + """
public class CompositionRoot : ICompositionRoot
{
    public void Compose(IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<DependencyInjection.SourceGenerator.Tests.IAutomaticlyGeneratedService, DependencyInjection.SourceGenerator.Tests.AutomaticlyGeneratedService>(new PerRequestLifeTime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }
}