using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyInjection.SourceGenerator.LightInject.Enums;

internal enum Lifetime
{
    Transient,
    Scoped,
    Singleton,
}
