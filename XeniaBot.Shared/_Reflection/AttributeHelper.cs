using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace XeniaBot.Shared
{
    public static class AttributeHelper
    {
        public static void InjectControllerAttributes(Assembly assembly, IServiceCollection services)
        {
            var classes = GetTypesWithAttribute<BotControllerAttribute>(assembly);
            foreach (var item in classes)
            {
                var descriptor = new ServiceDescriptor(item, item, ServiceLifetime.Singleton);
                if (services.Contains(descriptor))
                    continue;
                services.AddSingleton(item);
                Console.WriteLine($"Injected {item}");
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
                if (type.GetCustomAttributes(typeof(T), true).Length > 0 && type.IsAssignableTo(typeof(BaseController))) {
                    yield return type;
                }
            }
        }
    }
}