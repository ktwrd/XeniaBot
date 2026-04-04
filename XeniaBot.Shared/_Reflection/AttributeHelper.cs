using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace XeniaBot.Shared;

public static class AttributeHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static void InjectControllerAttributes(Assembly assembly, IServiceCollection services)
    {
        var classes = GetTypesWithAttribute<XeniaControllerAttribute>(assembly);
        foreach (var item in classes)
        {
            
            var registered = false;
            var descriptor = new ServiceDescriptor(item, item, ServiceLifetime.Singleton);
            if (typeof(BaseService).IsAssignableFrom(item))
            {
                var indirectDescriptor = new ServiceDescriptor(typeof(BaseService), item, ServiceLifetime.Singleton);
                if (!services.Contains(indirectDescriptor))
                {
                    services.AddSingleton(indirectDescriptor);
                    registered = true;
                }
            }

            if (!services.Contains(descriptor))
            {
                services.AddSingleton(item);
                registered = true;
            }

            if (registered)
            {
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
            if (type.GetCustomAttributes(typeof(T), true).Length > 0 && type.IsAssignableTo(typeof(BaseService))) {
                yield return type;
            }
        }
    }
}