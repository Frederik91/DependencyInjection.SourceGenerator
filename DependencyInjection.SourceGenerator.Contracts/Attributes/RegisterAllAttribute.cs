using DependencyInjection.SourceGenerator.Contracts.Attributes;
using DependencyInjection.SourceGenerator.Contracts.Enums;

namespace DependencyInjection.SourceGenerator.Contracts.Attributes;

public interface IRegisterAllAttribute
{
    Lifetime Lifetime { get;}
    bool IncludeServiceName { get; } 
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RegisterAllAttribute(Type serviceType) : Attribute, IRegisterAllAttribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public Type ServiceType { get; set; } = serviceType;

    public bool IncludeServiceName { get; set; }

    public RegisterAllAttribute(Type serviceType, Lifetime lifetime) : this(serviceType)
    {
        Lifetime = lifetime;
    }
}


[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RegisterAllAttribute<TServiceType> : Attribute, IRegisterAllAttribute
{
    public Lifetime Lifetime { get; set; } = Lifetime.Transient;
    public bool IncludeServiceName { get; set; }
}
