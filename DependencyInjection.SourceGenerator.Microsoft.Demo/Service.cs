using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;

namespace DependencyInjection.SourceGenerator.Microsoft.Demo;

[Register]
public class Service : IService { }

public interface IService { }

[Register(Lifetime = Lifetime.Scoped, ServiceName = "Test")]
public class Service2 : IService2 { }

public interface IService2 { }

[Register(Lifetime = Lifetime.Singleton, ServiceName = "Test", ServiceType = typeof(IService2))]
public class Service3 : IService, IService2 { }