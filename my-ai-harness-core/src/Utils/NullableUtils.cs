using System;
using System.ComponentModel;
using DynamicData.Kernel;

namespace Ablinger.MyAiHarness.Core.Utils;

public static class NullableUtils
{
    public static TResult Run<T, TResult>(this T self, Func<T, TResult> func)
    {
        return func(self);
    }

    public static void Run<T>(this T self, Action<T> action)
    {
        action(self);
    }

    public static void Run<T>(this Optional<T> self, Action<T> action) where T : notnull
    {
        if (self.HasValue)
        {
            action(self.Value);
        }
    }
}