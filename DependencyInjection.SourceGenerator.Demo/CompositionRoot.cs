using LightInject;

namespace DependencyInjection.SourceGenerator.Demo;

public partial class CompositionRoot
{
    public static void RegisterServices(IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<IService, Service>(new PerRequestLifeTime());
    }       
}
