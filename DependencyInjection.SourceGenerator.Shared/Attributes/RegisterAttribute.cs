using DependencyInjection.SourceGenerator.Shared.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Shared.Attributes;

internal sealed class RegisterAttribute : Attribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public string? ServiceName { get; set; }
    public Type? ServiceType { get; set; }
}
