# DependencyInjection.SourceGenerator.LightInject
Register services using attributes instead of registering in code.

## Usage
Add the "Register" attribute to the class you want to register. The attribute takes a type and a lifetime. The type is the type you want to register and the lifetime is the lifetime of the service. The lifetime is optional and defaults to Transient.

```csharp
var services = new ServiceCollection();
services.AddMyProject();

### LightInject

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

### Register all services in the project
You can also register all services in an project by adding the RegisterAll attribute to the assembly. This will register all implementations of the specified type.

```csharp

using DependencyInjection.SourceGenerator.Contracts.Attributes;

[assembly: RegisterAll<IExampleService>]

namespace RootNamespace.Services;

public interface IExampleService
{
	string GetExample();
}

public class ExampleService1 : IExampleService
{
	public string GetExample()
	{
		return "Example 1";
	}
}

public class ExampleService2 : IExampleService
{
	public string GetExample()
	{
		return "Example 2";
	}
}

```

this will generate the following code:

```csharp

public class CompositionRoot : ICompositionRoot
{
	public static void Compose(IServiceRegistry serviceRegistry)
	{
		serviceRegistry.Register<IExampleService, ExampleService1>(new PerContainerLifetime());
		serviceRegistry.Register<IExampleService, ExampleService2>(new PerContainerLifetime());		
	}
}
```

## Lifetime
The lifetime is an enum with the following values:
- Transient
- Scoped
- Singleton