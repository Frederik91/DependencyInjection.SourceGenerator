using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjection.SourceGenerator.Demo;
[AttributeUsage(AttributeTargets.Class)]
internal class GenerateAutomaticInterfaceAttribute : Attribute
{
}
