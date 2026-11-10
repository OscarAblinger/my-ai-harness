using System;

namespace Ablinger.MyAiHarness.Core.Harness.Database;

public interface IDbOptions<T>
{
    /// <summary>
    /// Creates and returns the default value if no entry was found in the DB.
    /// </summary>
    /// <param name="path">The full path of the value.</param>
    T CreateDefaultValue(string path);

    /// <summary>
    /// Hook to change or adjust the value of a freshly created or deserialised value.
    /// <list type="bullet">
    /// <item>
    /// Will be called directly after <see cref="CreateDefaultValue"/> or whenever a new value is deserialized from the
    /// DB.</item>
    /// <item>Will be called before the value is sent out to observers.</item>
    /// <item>Will NOT be called when the value is set from outside sources.</item>
    /// </list>
    /// </summary>
    T InitialiseNewValue(T value)
    {
        return value;
    }
}

public sealed class LambdaBasedDbOption<T>(Func<string, T> createDefaultValue, Func<T, T> initialiseNewValue)
    : IDbOptions<T>
{
    public T CreateDefaultValue(string path)
    {
        return createDefaultValue(path);
    }

    public T InitialiseNewValue(T value)
    {
        return initialiseNewValue(value);
    }
}