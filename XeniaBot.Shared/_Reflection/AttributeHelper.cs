using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XeniaBot.Shared;

public static class AttributeHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static void InjectControllerAttributes(Assembly assembly, IServiceCollection services)
    {
        var classes = GetTypesWithAttribute<XeniaControllerAttribute>(assembly);
        foreach (var item in classes)
        {
            var descriptor = new ServiceDescriptor(item, item, ServiceLifetime.Singleton);
            if (!services.Any(e
                => e.ServiceType == descriptor.ServiceType
                && e.ImplementationType == descriptor.ImplementationType
                && e.Lifetime == descriptor.Lifetime))
            {
                services.Add(descriptor);
                Log.Trace($"Registered type: {item}");
            }
        }
    }
    public static void InjectControllerAttributes(string name, IServiceCollection services)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var item in loadedAssemblies)
        {
            var nameSplit = item.FullName?.Split(',')[0];
            if (name == nameSplit)
                InjectControllerAttributes(item, services);
        }
    }
    public static IEnumerable<Type> GetTypesWithAttribute<T>(Assembly assembly)
    {
        foreach(Type type in assembly.GetTypes()) {
            if (type.GetCustomAttributes(typeof(T), true).Length > 0 && type.IsAssignableTo(typeof(IBaseService)) && type != typeof(IBaseService)) {
                yield return type;
            }
        }
    }
}