using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjection.SourceGenerator.Tests;
[AttributeUsage(AttributeTargets.Class)]
public class GenerateAutomaticInterfaceAttribute : Attribute
{
}
