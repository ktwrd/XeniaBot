using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace SkidBot.Shared
{
    public static class AttributeHelper
    {
        public static void InjectControllerAttributes(Assembly assembly, IServiceCollection services)
        {
            var classes = GetTypesWithAttribute<SkidControllerAttribute>(assembly);
            foreach (var item in classes)
            {
                services.AddSingleton(item);
                Console.WriteLine($"Injected {item}");
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