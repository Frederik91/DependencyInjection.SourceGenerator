using DependencyInjection.SourceGenerator.Contracts.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Class)]
[Decorate<DecorateAttribute>]
public class DecorateAttribute : Attribute
{
    public Type? ServiceType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class DecorateAttribute<TServiceType> : Attribute
{
}