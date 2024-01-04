using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyInjection.SourceGenerator.Shared;
internal static class RegistrationCollector
{
    internal static List<INamedTypeSymbol> GetTypes(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ClassAttributeReceiver receiver)
        {
            return [];
        }

        // Get all types with the "Inject" attribute
        return receiver.Classes;
    }
}
