using System;
using System.ComponentModel;

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
}