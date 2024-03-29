﻿using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationCollector
{
    internal static List<INamedTypeSymbol> GetTypes(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ClassAttributeReceiver receiver)
        {
            return [];
        }

        return receiver.Types;
    }


    internal static List<Registration> GetRegisterAllTypes(GeneratorExecutionContext context)
    {
        var registerAllAttributes = TypeHelper.GetAttributes<RegisterAllAttribute>(context.Compilation.Assembly.GetAttributes());

        var result = new List<Registration>();
        foreach (var registerAllAttribute in registerAllAttributes)
        {
            var serviceType = TypeHelper.GetServiceTypeFromAttribute(registerAllAttribute);
            if (serviceType is null)
                continue;

            var lifetime = TypeHelper.GetLifetimeFromAttribute(registerAllAttribute) ?? Lifetime.Transient;

            var includeServiceName = TypeHelper.GetAttributeValue(registerAllAttribute, nameof(RegisterAllAttribute.IncludeServiceName));
            if (!bool.TryParse(includeServiceName?.ToString(), out var includeServiceNameValue))
                includeServiceNameValue = false;

            var registrations = GetImplementedTypes(serviceType, context, lifetime, includeServiceNameValue);
            result.AddRange(registrations);
        }

        return result;
    }

    private static List<Registration> GetImplementedTypes(INamedTypeSymbol? serviceType, GeneratorExecutionContext context, Lifetime lifetime, bool includeServiceNameValue)
    {
        var result = new List<Registration>();
        if (serviceType is null)
            return result;

        var compilation = context.Compilation;
        foreach (var typeName in compilation.Assembly.TypeNames)
        {
            var types = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type).OfType<INamedTypeSymbol>();
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                var registration = GetRegistration(type, serviceType, lifetime, includeServiceNameValue);
                if (registration is not null)
                    result.Add(registration);                
            }
        }

        return result;
    }

    private static Registration? GetRegistration(INamedTypeSymbol type, INamedTypeSymbol serviceType, Lifetime lifetime, bool includeServiceNameValue)
    {
        if (serviceType.TypeKind == TypeKind.Interface && GetRegistrationByInterface(type, serviceType, lifetime, includeServiceNameValue) is { } registration)
            return registration;

        if (serviceType.TypeKind == TypeKind.Class && GetBaseTypeImplementation(type, serviceType) is { } baseTypeImplementation)
        {
            return new Registration
            {
                ImplementationTypeName = TypeHelper.GetFullName(type),
                Lifetime = lifetime,
                ServiceName = includeServiceNameValue ? type.Name : null,
                ServiceType = TypeHelper.GetFullName(baseTypeImplementation, type.ContainingNamespace)
            };
        }
        return null;
    }

    private static Registration? GetRegistrationByInterface(INamedTypeSymbol type, INamedTypeSymbol serviceType, Lifetime lifetime, bool includeServiceNameValue)
    {
        var typeName = TypeHelper.GetFullName(type);
        var serviceTypeName = TypeHelper.GetFullName(serviceType, type.ContainingNamespace);
        foreach (var implInterface in type.AllInterfaces)
        {
            var implInterfaceName = TypeHelper.GetFullName(implInterface, type.ContainingNamespace);
            if (serviceType.IsUnboundGenericType)
            {
                if (implInterface.IsGenericType && TypeHelper.GetFullName(implInterface.ConstructUnboundGenericType(), type.ContainingNamespace) == serviceTypeName)
                {
                    return new Registration
                    {
                        ImplementationTypeName = typeName,
                        Lifetime = lifetime,
                        ServiceName = includeServiceNameValue ? type.Name : null,
                        ServiceType = implInterfaceName
                    };
                }
            }
            else if (implInterfaceName == serviceTypeName)
            {
                return new Registration
                {
                    ImplementationTypeName = typeName,
                    Lifetime = lifetime,
                    ServiceName = includeServiceNameValue ? type.Name : null,
                    ServiceType = serviceTypeName
                };
            }
        }
        return null;
    }

    private static INamedTypeSymbol? GetBaseTypeImplementation(INamedTypeSymbol type, INamedTypeSymbol typeToCheck)
    {
        if (type.BaseType is not { } baseType)
            return null; ;

        if (typeToCheck.IsUnboundGenericType)
        {
            if (baseType.IsGenericType && TypeHelper.GetFullName(baseType.ConstructUnboundGenericType(), type.ContainingNamespace) == TypeHelper.GetFullName(typeToCheck, type.ContainingNamespace))
                return baseType;

            return GetBaseTypeImplementation(baseType, typeToCheck);
        }

        if (TypeHelper.GetFullName(baseType, type.ContainingNamespace) == TypeHelper.GetFullName(typeToCheck, type.ContainingNamespace))
            return baseType;

        return GetBaseTypeImplementation(baseType, typeToCheck);
    }
}
