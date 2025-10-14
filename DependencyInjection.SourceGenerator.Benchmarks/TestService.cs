using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Benchmarks;

#pragma warning disable CS9113 

[Register]
public class TestService
{
    public TestService(TestDependency _, [FactoryArgument] int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[Register]
public class TestDependency(IServiceProvider _) { };
