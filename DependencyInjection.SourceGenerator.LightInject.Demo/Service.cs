using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

[Register]
public class Service : IService
{
}

public interface IService
{
    
}

[Register(Lifetime = Lifetime.Scoped, ServiceName = "Test")]
public class Service2 : IService2
{
}

public interface IService2
{

}
