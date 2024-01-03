using DependencyInjection.SourceGenerator.LightInject.Enums;
using System;

namespace DependencyInjection.SourceGenerator.LightInject.Attributes;

internal class RegisterAttribute : Attribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public string? ServiceName { get; set; }
    public Type? ServiceType { get; set; }
}
