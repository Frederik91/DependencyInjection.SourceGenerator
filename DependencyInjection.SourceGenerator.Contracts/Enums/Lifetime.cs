using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyInjection.SourceGenerator.Contracts.Enums;

public enum Lifetime
{
    Transient,
    Scoped,
    Singleton,
}
