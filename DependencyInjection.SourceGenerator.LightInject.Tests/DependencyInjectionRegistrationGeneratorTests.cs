using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using VerifyCS = DependencyInjection.SourceGenerator.LightInject.Tests.CSharpSourceGeneratorVerifier<DependencyInjection.SourceGenerator.LightInject.DependencyInjectionRegistrationGenerator>;
using Microsoft.CodeAnalysis.Testing;

namespace DependencyInjection.SourceGenerator.LightInject.Tests;

public class DependencyInjectionRegistrationGeneratorTests
{
    private static readonly string _header = """
        // <auto-generated/>
        #pragma warning disable
        #nullable enable
        namespace DependencyInjection.SourceGenerator.LightInject.Demo;
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        
        """;

    private readonly ImmutableArray<string> _references = AppDomain.CurrentDomain
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

        tester.ReferenceAssemblies.AddAssemblies(_references);
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

        var expected = _header + """
public class CompositionRoot : global::LightInject.ICompositionRoot
{
    public void Compose(global::LightInject.IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<global::DependencyInjection.SourceGenerator.LightInject.Demo.IService, global::DependencyInjection.SourceGenerator.LightInject.Demo.Service>(new global::LightInject.PerRequestLifeTime());
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

public partial class CompositionRoot : global::LightInject.ICompositionRoot
{
    public static void RegisterServices(global::LightInject.IServiceRegistry serviceRegistry)
    {
        
    } 
}

""";

        var expected = _header + """
public partial class CompositionRoot : global::LightInject.ICompositionRoot
{
    public void Compose(global::LightInject.IServiceRegistry serviceRegistry)
    {
        RegisterServices(serviceRegistry);
        serviceRegistry.Register<global::DependencyInjection.SourceGenerator.LightInject.Demo.IService, global::DependencyInjection.SourceGenerator.LightInject.Demo.Service>(new global::LightInject.PerRequestLifeTime());
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

        var expected = _header + """
public class CompositionRoot : global::LightInject.ICompositionRoot
{
    public void Compose(global::LightInject.IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<global::DependencyInjection.SourceGenerator.LightInject.Demo.IService, global::DependencyInjection.SourceGenerator.LightInject.Demo.Service>("Test", new global::LightInject.PerScopeLifetime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }

    [Fact]
    public async Task Register_Specified_ServiceType()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Register(ServiceType = typeof(Service))]
public class Service : IService {}
public interface IService {}

""";

        var expected = _header + """
public class CompositionRoot : global::LightInject.ICompositionRoot
{
    public void Compose(global::LightInject.IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<global::DependencyInjection.SourceGenerator.LightInject.Demo.Service>(new global::LightInject.PerRequestLifeTime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }

    [Fact]
    public async Task Register_NoInteface()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Register]
public class Service {}

""";

        var expected = _header + """
public class CompositionRoot : global::LightInject.ICompositionRoot
{
    public void Compose(global::LightInject.IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<global::DependencyInjection.SourceGenerator.LightInject.Demo.Service>(new global::LightInject.PerRequestLifeTime());
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }

    [Fact]
    public async Task CreateCompositionRoot_DecorateService_NoExistingCompositionRoot()
    {
        var code = """
using DependencyInjection.SourceGenerator.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Decorate]
public class Service : IService {}
public interface IService {}

""";

        var expected = _header + """
public class CompositionRoot : global::LightInject.ICompositionRoot
{
    public void Compose(global::LightInject.IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Decorate<global::DependencyInjection.SourceGenerator.LightInject.Demo.IService, global::DependencyInjection.SourceGenerator.LightInject.Demo.Service>();
    }
}
""";

        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }
}