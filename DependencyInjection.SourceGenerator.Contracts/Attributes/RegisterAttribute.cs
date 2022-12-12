using DependencyInjection.SourceGenerator.Contracts.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Contracts.Attributes;

public class RegisterAttribute : Attribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public string? ServiceName { get; set; }
}
