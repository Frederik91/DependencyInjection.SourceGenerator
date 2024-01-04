using DependencyInjection.SourceGenerator.Contracts.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Shared;

internal sealed class Decoration
{
    public required string DecoratedTypeName { get; init; }
    public required string DecoratorTypeName { get; init; }
}
