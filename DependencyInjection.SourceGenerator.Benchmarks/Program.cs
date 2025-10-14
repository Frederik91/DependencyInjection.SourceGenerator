using BenchmarkDotNet.Running;

namespace DependencyInjection.SourceGenerator.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<FactoryBenchmarks>();
    }
}
