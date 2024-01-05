using DependencyInjection.SourceGenerator.Contracts.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Contracts.Attributes;
public interface IRegisterAttribute
{
    public Lifetime Lifetime { get; }
    public string? ServiceName { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public class RegisterAttribute : Attribute, IRegisterAttribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public string? ServiceName { get; set; }
    public Type? ServiceType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class RegisterAttribute<TServiceType> : Attribute, IRegisterAttribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public string? ServiceName { get; set; }
}
