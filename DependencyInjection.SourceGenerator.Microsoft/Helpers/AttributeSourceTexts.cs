namespace DependencyInjection.SourceGenerator.Microsoft.Helpers;

public static class AttributeSourceTexts
{
    public static string CreateDefaultServiceRegistrationsClassText(string assemblyName) => 
    $$"""
#nullable enable
namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static partial class ServiceCollectionExtensions
    {
        public static partial global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{{assemblyName}}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            return services;
        }
    }

    public static partial class ServiceCollectionExtensions
    {
        public static partial global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{{assemblyName}}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services);
    }
}
""";

    public const string RegisterAttributeText = @"
#nullable enable
namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class RegisterAttribute : global::System.Attribute
    {
        public global::Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; } = global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;
        public string? ServiceName { get; set; }
        public bool IncludeFactory { get; set; }
        public global::System.Type? ServiceType { get; set; }
    }

    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class RegisterAttribute<TServiceType> : global::System.Attribute
    {
        public global::Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; } = global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;
        public string? ServiceName { get; set; }
        public bool IncludeFactory { get; set; }
    }
}";

    public const string FactoryArgumentAttributeText = @"
#nullable enable
namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Parameter)]
    internal sealed class FactoryArgumentAttribute : global::System.Attribute
    {
    }
}";

    public const string RegisterAllAttributeText = @"
#nullable enable
namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class RegisterAllAttribute : global::System.Attribute
    {
        public global::Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; } = global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;
        public global::System.Type ServiceType { get; set; }
        public bool IncludeServiceName { get; set; }
        public bool IncludeFactory { get; set; }

        public RegisterAllAttribute(global::System.Type serviceType)
        {
            ServiceType = serviceType;
        }

        public RegisterAllAttribute(global::System.Type serviceType, global::Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) : this(serviceType)
        {
            Lifetime = lifetime;
        }
    }

    [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class RegisterAllAttribute<TServiceType> : global::System.Attribute
    {
        public global::Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; } = global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;
        public bool IncludeServiceName { get; set; }
        public bool IncludeFactory { get; set; }
    }
}";

    public const string DecorateAttributeText = @"
#nullable enable
namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Class)]
    internal sealed class DecorateAttribute : global::System.Attribute
    {
        public global::System.Type? ServiceType { get; set; }
    }

    [global::System.AttributeUsage(global::System.AttributeTargets.Class)]
    internal sealed class DecorateAttribute<TServiceType> : global::System.Attribute
    {
    }
}";
}
