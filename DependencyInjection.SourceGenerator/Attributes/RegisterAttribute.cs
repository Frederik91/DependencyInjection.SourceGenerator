using DependencyInjection.SourceGenerator.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Attributes;

internal class RegisterAttribute : Attribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public string? ServiceName { get; set; }
}
