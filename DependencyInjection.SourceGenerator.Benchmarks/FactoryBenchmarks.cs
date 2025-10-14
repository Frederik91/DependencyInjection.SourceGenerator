using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.SourceGenerator.Benchmarks;

[MemoryDiagnoser]
public class FactoryBenchmarks
{
    private ITestServiceFactory _factory = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // The generator will emit an extension named based on the assembly name
        // For this project the generator produces: AddDependencyInjectionSourceGeneratorBenchmarks()
        services.AddDependencyInjectionSourceGeneratorBenchmarks();
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<ITestServiceFactory>();
    }

    [Benchmark]
    public TestService CreateWithFactory()
    {
        return _factory.Create(42);
    }
}
