using DependencyInjection.SourceGenerator.Contracts.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Contracts.Attributes;

public class DecorateAttribute : Attribute
{
    public Type? ServiceType { get; set; }
}
