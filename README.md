# DependencyInjection.SourceGenerator
Register services using attributes instead of registering in code.

## Usage
Add the "Register" attribute to the class you want to register. The attribute takes a type and a lifetime. The type is the type you want to register and the lifetime is the lifetime of the service. The lifetime is optional and defaults to Transient.

This library supports the following dependency injection frameworks, follow the links for more information on how to use them:
- [Microsoft.Extensions.DependencyInjection](DependencyInjection.SourceGenerator.Microsoft/readme.md)
- [LightInject](DependencyInjection.SourceGenerator.LightInject/readme.md)

To use this library you need to install the source generator package and the contacts package. 
The source generator package is a development dependency and will not be exposed as a dependency to consumers of your projects, while the contracts package contains the attributes and enums used to configure the generator.

### Microsoft.Extensions.DependencyInjection
* #### Microsoft.Extensions.DependencyInjection.Microsoft [![NuGet](https://img.shields.io/nuget/vpre/DependencyInjection.SourceGenerator.Microsoft.svg)](https://www.nuget.org/packages/DependencyInjection.SourceGenerator.Microsoft)
* #### Microsoft.Extensions.DependencyInjection.Microsoft.Contracts [![NuGet](https://img.shields.io/nuget/vpre/DependencyInjection.SourceGenerator.Microsoft.Contracts.svg)](https://www.nuget.org/packages/DependencyInjection.SourceGenerator.Microsoft.Contracts)

### LightInject
* #### Microsoft.Extensions.DependencyInjection.LightInject [![NuGet](https://img.shields.io/nuget/vpre/DependencyInjection.SourceGenerator.LightInject.svg)](https://www.nuget.org/packages/DependencyInjection.SourceGenerator.LightInject)
* #### Microsoft.Extensions.DependencyInjection.LightInject.Contracts [![NuGet](https://img.shields.io/nuget/vpre/DependencyInjection.SourceGenerator.LightInject.Contracts.svg)](https://www.nuget.org/packages/DependencyInjection.SourceGenerator.LightInject.Contracts)

Both contracts packages references the shared contracts package, which contains the attributes and enums used to configure the generator.
* #### Microsoft.Extensions.DependencyInjection.Contracts [![NuGet](https://img.shields.io/nuget/vpre/DependencyInjection.SourceGenerator.Contracts.svg)](https://www.nuget.org/packages/DependencyInjection.SourceGenerator.Contracts)

## Lifetime
The lifetime is an enum with the following values:
- Transient
- Scoped
- Singleton