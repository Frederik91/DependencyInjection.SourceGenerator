# DependencyInjection.SourceGenerator
Register services using attributes instead of registering in code.

## Usage
Add the "Register" attribute to the class you want to register. The attribute takes a type and a lifetime. The type is the type you want to register and the lifetime is the lifetime of the service. The lifetime is optional and defaults to Transient.

```csharp
[Register(ServiceName = "ServiceName", Lifetime = Lifetime.Singleton)]
public class ExampleService : IExampleService
{
	public string GetExample()
	{
		return "Example";
	}
}

public interface IExampleService
{
	string GetExample();
}

```

Generates a class CompositionRoot

```csharp
public class CompositionRoot : ICompositionRoot
{
	public static void Compose(IServiceRegistry serviceRegistry)
	{
		serviceRegistry.Register<IExampleService, ExampleService>("ServiceName", new PerContainerLifetime());
	}
}
```

If you already have a class CompositionRoot defined, the generated class will be made partial. Remeber to make your CompositionRoot partial as well.
It will also call a method RegisterServices on the existing CompositionRoot class (this must be defined).

```csharp
public partial class CompositionRoot : ICompositionRoot
{
	public static void Compose(IServiceRegistry serviceRegistry)
	{
		RegisterServices(serviceRegistry);
		serviceRegistry.Register<IExampleService, ExampleService>("ServiceName", new PerContainerLifetime());
	}
}
```

The final existing CompositionRoot class must look like this:

```csharp
public partial class CompositionRoot
{
	public void RegisterServices(IServiceRegistry serviceRegistry)
	{
		// Register services here
	}
}
```

## Lifetime
The lifetime is an enum with the following values:
- Transient
- Scoped
- Singleton


## Misc
Current only works with LightInject.