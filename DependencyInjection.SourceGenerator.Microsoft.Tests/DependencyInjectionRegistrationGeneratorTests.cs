using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.CompilerServices;
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Tests;

public class DependencyInjectionRegistrationGeneratorTests
{
    private static async Task RunTestAsync(string code, [CallerMemberName] string methodName = "", bool validateCompilation = true)
    {
        List<MetadataReference> references = [];

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .ToList();

        var dependencyInjectionAssembly = typeof(ServiceLifetime).Assembly;
        if (!assemblies.Contains(dependencyInjectionAssembly))
            assemblies.Add(dependencyInjectionAssembly);

        var scrutorAssembly = typeof(Scrutor.DecorationStrategy).Assembly;
        if (!assemblies.Contains(scrutorAssembly))
            assemblies.Add(scrutorAssembly);

        foreach (var assemblyPath in assemblies)
        {
            references.Add(MetadataReference.CreateFromFile(assemblyPath.Location));
        }

        var syntax = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(
            "TestProject",
            syntaxTrees: [syntax],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new DependencyInjectionRegistrationGenerator());

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> generatorDiagnostics);

        if (validateCompilation)
        {
            var diagnostics = outputCompilation.GetDiagnostics();
            diagnostics.Should().BeEmpty("because there should be no compilation errors after running the generator");
        }

        foreach (var syntaxTree in outputCompilation.SyntaxTrees)
        {
            var fileName = Path.GetFileName(syntaxTree.FilePath);
            if (fileName is null)
            {
                continue;
            }

            if (fileName.EndsWith("ServiceRegistrations.g.cs", StringComparison.Ordinal) == false &&
                fileName.EndsWith("Factory.g.cs", StringComparison.Ordinal) == false)
            {
                continue;
            }

            var generatedSource = syntaxTree.ToString().Replace("\r\n", "\n");
            var settings = new VerifySettings();
            settings.UseDirectory("TestResults");
            settings.UseFileName(methodName + "_" + Path.GetFileNameWithoutExtension(fileName));
            await Verifier.Verify(generatedSource, settings);
        }
    }

    [Theory]
    [InlineData("Test", "Test")]
    [InlineData("Test.abc", "TestAbc")]
    [InlineData("Test-abc", "TestAbc")]
    [InlineData("Test_abc", "TestAbc")]
    public void GetSafeMethodName(string assemblyName, string expectedMethodName)
    {
        DependencyInjectionRegistrationGenerator.EscapeAssemblyNameToMethodName(assemblyName).Should().Be(expectedMethodName);
    }

    [Fact]
    public async Task Register_DefaultValues()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Register]
public class Service : IService {}
public interface IService {}

""";

        await RunTestAsync(code);
    }


    [Fact]
    public async Task Register_Record()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Register]
public sealed record Service;
""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_WithFactory()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Register(IncludeFactory = true)]
public class Service : IService {}
public interface IService {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_WithFactoryArguments()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Register(ServiceType = typeof(IReportJob), Lifetime = ServiceLifetime.Scoped)]
public sealed class ReportJob : IReportJob
{
    public ReportJob(IDependency dependency, [FactoryArgument] System.Guid reportId)
    {
    }
}

public interface IReportJob
{
}

public interface IDependency
{
}
""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_WithCollection()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class Test 
{
    [Register]
    public static void RegisterMethod(IServiceCollection services)
    {
    }
}
""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_MultipleServices()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

    [Register(ServiceType = typeof(IService1))]
    [Register(ServiceType = typeof(IService2))]
    public class Service : IService1, IService2 {}
    public interface IService1 {}
    public interface IService2 {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_UndefinedService()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class Base {}

[Register]
public class Service1 : Base, IService {}

[Register<IService>]
public class Service2 : IService {}

[Register(typeof(IService))]
public class Service3 : IService {}

""";

        await RunTestAsync(code, validateCompilation: false);
    }

    [Fact]
    public async Task Register_ScopedLifetime_And_ServiceName()
    {
        var code = """
    using global::Microsoft.Extensions.DependencyInjection;

    namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

    [Register(Lifetime = ServiceLifetime.Scoped, ServiceName = "Test")]
    public class Service : IService {}
    public interface IService {}

    """;

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_Specified_ServiceType()
    {
        var code = """
    using global::Microsoft.Extensions.DependencyInjection;

    namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

    [Register(ServiceType = typeof(Service<string>))]
    public class Service<T> : IService {}
    public interface IService {}

    """;

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_NoInterface_Or_BaseClass()
    {
        var code = """
    using global::Microsoft.Extensions.DependencyInjection;


    namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

    [Register]
    public class Service {}

    """;

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Register_Specified_ServiceType_UsingGeneric()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Register<IService2>]
public class Service : IService1, IService2 {}
public interface IService1 {}
public interface IService2 {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Decorate_DefaultValues()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Decorate]
public class Service : IService {}
public interface IService {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Decorate_Specified_ServiceType()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Decorate(ServiceType = typeof(IService2))]
public class Service : IService1, IService2 {}
public interface IService1 {}
public interface IService2 {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task Decorate_Specified_ServiceType_UsingGeneric()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Decorate<IService2>]
public class Service : IService1, IService2 {}
public interface IService1 {}
public interface IService2 {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task RegisterAll_ByInterface()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

[assembly: RegisterAll<global::DependencyInjection.SourceGenerator.Microsoft.Demo.IServiceA>]
[assembly: RegisterAll<global::DependencyInjection.SourceGenerator.Microsoft.Demo.IServiceB>]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class ServiceA1 : IServiceA {}
public class ServiceA2 : IServiceA {}
public interface IServiceA {}

public class ServiceB1 : IServiceB {}
public class ServiceB2 : IServiceB {}
public interface IServiceB {}

""";

        await RunTestAsync(code);
    }


    [Fact]
    public async Task RegisterAll_SpecifyLifetime()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

[assembly: RegisterAll<global::DependencyInjection.SourceGenerator.Microsoft.Demo.IService>(Lifetime = ServiceLifetime.Singleton)]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class Service1 : IService {}
public class Service2 : IService {}
public interface IService {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task RegisterAll_ByBaseType_WithServiceName()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

[assembly: RegisterAll<global::DependencyInjection.SourceGenerator.Microsoft.Demo.MyBase>(IncludeServiceName = true)]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class Service1 : MyBase {}
public class Service2 : MyBase {}
public abstract class MyBase {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task RegisterAll_ByBaseType_WithoutServiceName()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;

[assembly: RegisterAll<global::DependencyInjection.SourceGenerator.Microsoft.Demo.MyBase>]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class Service1 : MyBase {}
public class Service2 : MyBase {}
public abstract class MyBase {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task RegisterAll_GenericInterfaceType()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;
using global::DependencyInjection.SourceGenerator.Microsoft.Demo;

[assembly: RegisterAll(typeof(IService<>))]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public interface IDummy { }
public interface IService<TType> { }
public class Service1 : IDummy, IService<string> {}
public class Service2 : IService<int> {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task RegisterAll_GenericBaseType()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;
using global::DependencyInjection.SourceGenerator.Microsoft.Demo;

[assembly: RegisterAll(typeof(Base<>))]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public abstract class Base<TType> { }
public class Service1 : Base<string> {}
public class Service2 : Base<int> {}

""";

        await RunTestAsync(code);
    }


    [Fact]
    public async Task RegisterAll_GenericBaseClassType()
    {
        var code = """
using global::Microsoft.Extensions.DependencyInjection;
using global::DependencyInjection.SourceGenerator.Microsoft.Demo;

[assembly: RegisterAll(typeof(BaseType<>))]

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public abstract class MyType : BaseType<string> { }
public abstract class BaseType<TType> { }
public class Service : MyType {}

""";

        await RunTestAsync(code);
    }

    [Fact]
    public async Task RegisterMethod_DefaultValues()
    {
        var code =
"""
using global::Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

public class Test 
{
    [Register]
    public static IService RegisterMethod(System.IServiceProvider services)
    {
        return new Service();
    }
}
public class Service : IService {}
public interface IService {}

""";

        await RunTestAsync(code);
    }
}