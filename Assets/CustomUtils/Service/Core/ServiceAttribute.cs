using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute {

    public readonly HashSet<Enum> serviceTypeSet = new();

    public ServiceAttribute() => serviceTypeSet.Add(DEFAULT_SERVICE_TYPE.NONE);

    public ServiceAttribute(params object[] serviceTypes) {
        foreach (var type in serviceTypes) {
            if (type is Enum enumType) {
                serviceTypeSet.Add(enumType);
            }
        }
    }

    public bool Contains(Enum serviceType) => serviceTypeSet.Contains(serviceType);
    public bool Contains(params Enum[] serviceTypes) => serviceTypes.Any(type => serviceTypeSet.Contains(type));
    public bool Contains<TEnum>(TEnum serviceType) where TEnum : struct, Enum => serviceTypeSet.Contains(serviceType);
    public bool Contains<TEnum>(params TEnum[] serviceTypes) where TEnum : struct, Enum => serviceTypes.Any(type => serviceTypeSet.Contains(type));
}