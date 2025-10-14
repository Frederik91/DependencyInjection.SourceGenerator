using DependencyInjection.SourceGenerator.Microsoft.Enums;
using Microsoft.CodeAnalysis;

namespace DependencyInjection.SourceGenerator.Microsoft.Helpers;

internal sealed class ClassRegistration
{
    public required string? ServiceType { get; init; }
    public required string? ServiceName { get; init; }
    public required bool IncludeFactory { get; set; }
    public required ServiceLifetime Lifetime { get; init; }
    public required string ImplementationTypeName { get; init; }
    public required INamedTypeSymbol ImplementationTypeSymbol { get; init; }
    public required INamedTypeSymbol? ServiceTypeSymbol { get; init; }
}

internal sealed class MethodFactoryRegistration
{
    public required string ServiceType { get; init; }
    public required string? ServiceName { get; init; }
    public required ServiceLifetime Lifetime { get; init; }
    public required string MethodClassName { get; init; }
    public required string MethodName { get; init; }
}

internal sealed class MethodCollectionRegistration
{
    public required string MethodClassName { get; init; }
    public required string MethodName { get; init; }
}

internal sealed record FactoryParameter(string TypeName, string Name);

internal sealed class FactoryRegistration
{
    public required string Namespace { get; init; }
    public required string InterfaceName { get; init; }
    public required string ClassName { get; init; }
    public required string ServiceTypeName { get; init; }
    public required string ImplementationTypeName { get; init; }
    public required IReadOnlyList<FactoryParameter> Parameters { get; init; }
    public required INamedTypeSymbol ImplementationTypeSymbol { get; init; }
    public required INamedTypeSymbol ServiceTypeSymbol { get; init; }
}