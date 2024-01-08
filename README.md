# DependencyInjection.SourceGenerator
Register services using attributes instead of registering in code.

## Usage
Add the "Register" attribute to the class you want to register. The attribute takes a type and a lifetime. The type is the type you want to register and the lifetime is the lifetime of the service. The lifetime is optional and defaults to Transient.

This library supports the following dependency injection frameworks, follow the links for more information on how to use them:
- [Microsoft.Extensions.DependencyInjection](DependencyInjection.SourceGenerator.Microsoft/readme.md)
- [LightInject](DependencyInjection.SourceGenerator.LightInject/readme.md)

## Lifetime
The lifetime is an enum with the following values:
- Transient
- Scoped
- Singleton