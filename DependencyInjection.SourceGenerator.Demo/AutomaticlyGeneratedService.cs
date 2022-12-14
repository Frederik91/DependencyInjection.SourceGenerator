using DependencyInjection.SourceGenerator.Contracts.Attributes;

namespace DependencyInjection.SourceGenerator.Demo;
[GenerateAutomaticInterface]
[Register]
internal class AutomaticlyGeneratedService : IAutomaticlyGeneratedService
{
    public void DoSomething()
    {
    }
}
