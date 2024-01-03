using LightInject;

namespace DependencyInjection.SourceGenerator.LightInject.Demo;

public partial class CompositionRoot
{
    public static void RegisterServices(IServiceRegistry serviceRegistry)
    {
        serviceRegistry.Register<IService, Service>(new PerRequestLifeTime());
    }       
}
