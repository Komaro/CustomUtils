using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute {

    public readonly Enum[] serviceTypes;

    public ServiceAttribute() => serviceTypes = new Enum[] { DEFAULT_SERVICE_TYPE.NONE };
    public ServiceAttribute(params object[] serviceTypes) => this.serviceTypes = serviceTypes.OfType<Enum>().ToArray();

    public bool Contains(Enum serviceType) => serviceTypes.Contains(serviceType);
    public bool Contains(params Enum[] serviceTypes) => serviceTypes.Any(serviceTypes.Contains);
    public bool Contains<TEnum>(TEnum serviceType) where TEnum : struct, Enum => serviceTypes.Contains(serviceType);
    public bool Contains<TEnum>(params TEnum[] serviceTypes) where TEnum : Enum => serviceTypes.Any(serviceTypes.Contains);
}