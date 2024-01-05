using DependencyInjection.SourceGenerator.Contracts.Enums;
using System;

namespace DependencyInjection.SourceGenerator.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RegistrationExtensionAttribute : Attribute
{
}