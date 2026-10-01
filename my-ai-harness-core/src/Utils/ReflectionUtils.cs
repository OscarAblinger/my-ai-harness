using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Ablinger.MyAiHarness.Core.Utils;

public static class ReflectionUtils
{
    public static T? FindImplementation<T>(ServiceProvider serviceProvider)
        where T : class
    {
        return FindImplementation<T>(AppDomain.CurrentDomain.GetAssemblies(), serviceProvider);
    }

    public static T? FindImplementation<T>(IEnumerable<Assembly> assemblies, ServiceProvider serviceProvider)
        where T : class
    {
        var wantedInterface = typeof(T);
        var implementations =
            (from assembly in assemblies
                from type in assembly.GetTypes()
                where wantedInterface.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract
                select type).ToList();
        return implementations.Count switch
        {
            > 1 => throw new TooManyImplementationsException(wantedInterface, implementations),
            0 => null,
            _ => (T?)ActivatorUtilities.CreateInstance(serviceProvider, implementations[0])
        };
    }

    public class TooManyImplementationsException(Type wantedInterface, List<Type> types) :
        Exception($"Found multiple possible implementations for {wantedInterface}: {types}");
}