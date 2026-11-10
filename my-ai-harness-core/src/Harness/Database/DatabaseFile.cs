using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ablinger.MyAiHarness.Core.Harness.Database;

public interface IDatabaseFile<out T> : IDisposable
{
    IObservable<T> AsObservable { get; }
}

public class AbstractDatabaseFile<T> : IDatabaseFile<T>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };

    private CompositeDisposable disposables = new();
    private readonly IFileAccess fileAccess;
    private readonly DatabaseBinder databaseBinder;
    private readonly string path;
    private readonly IDbOptions<T> dbOptions;

    private BehaviorSubject<(T, JsonNode)>? currentValueSubject;

    protected AbstractDatabaseFile(IFileAccess fileAccess, DatabaseBinder databaseBinder, string path,
        IDbOptions<T> dbOptions)
    {
        this.fileAccess = fileAccess;
        this.databaseBinder = databaseBinder;
        this.path = path;
        this.dbOptions = dbOptions;
    }

    public IObservable<T> AsObservable => Observable.Defer(GetCurrentValueSubject).Select(tuple => tuple.Item1);

    private BehaviorSubject<(T, JsonNode)> GetCurrentValueSubject()
    {
        currentValueSubject ??= new(GetOrWriteValueFromFile());
        return currentValueSubject;
    }

    protected bool HasValue(T value)
    {
        if (currentValueSubject == null || value == null)
        {
            return false;
        }

        return value.Equals(currentValueSubject.Value.Item1);
    }

    public void Dispose()
    {
        currentValueSubject?.Dispose();
        disposables.Dispose();
    }

    /// <exception cref="AbstractDatabaseFile{T}.InvalidDatabasePath">The given path is not valid.</exception>
    /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
    /// <exception cref="NotSupportedException">If type `T` cannot be serialised or deserialised.</exception>
    private (T, JsonNode) GetOrWriteValueFromFile()
    {
        if (TryGetFromFile(out var fromFile))
        {
            return ((T, JsonNode))fromFile;
        }

        var value = dbOptions.InitialiseNewValue(dbOptions.CreateDefaultValue(path));
        BindObject(value);
        Save(value);
        return (value, JsonSerializer.SerializeToNode(value, JsonSerializerOptions)!);
    }

    protected void SetValue(T value)
    {
        if (currentValueSubject != null && currentValueSubject.TryGetValue(out var currValue) &&
            ReferenceEquals(currValue.Item1, value))
        {
            return;
        }

        BindObject(value);
        Save(value);
        Invalidate();
    }

    private bool TryGetFromFile([NotNullWhen(true)] out (T, JsonNode)? fromFile)
    {
        try
        {
            var readAllText = fileAccess.ReadAllText(path);
            try
            {
                var deserializedNode = JsonSerializer.Deserialize<JsonNode>(readAllText, JsonSerializerOptions);
                if (deserializedNode != null)
                {
                    var deserializedValue = deserializedNode.Deserialize<T>(JsonSerializerOptions);
                    if (deserializedValue != null)
                    {
                        dbOptions.InitialiseNewValue(deserializedValue);
                        BindObject(deserializedValue);
                        fromFile = (deserializedValue, deserializedNode);
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // The JSON is invalid.
                // -or-
                // 'TValue' is not compatible with the JSON.
                // -or-
                // There is remaining data in the string beyond a single JSON value.
            }
        }
        catch (Exception exception)
            when (exception is ArgumentNullException or PathTooLongException or UnauthorizedAccessException
                      or NotSupportedException or SecurityException or ArgumentException)
        {
            throw new InvalidDatabasePath(path, exception);
        }
        catch (Exception exception)
            when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
        }

        fromFile = null;
        return false;
    }

    private void BindObject(T deserializedValue)
    {
        if (deserializedValue is ObservableObject observableValue)
        {
            disposables.Add(databaseBinder.BindObject(observableValue, WriteChangedValue));
        }
    }

    private void Save(T value)
    {
        Path.GetDirectoryName(path)?.Run(fileAccess.CreateDirectory);
        var serializedValue = JsonSerializer.Serialize(value, JsonSerializerOptions);
        fileAccess.WriteAllText(path, serializedValue);
    }

    protected void Invalidate()
    {
        if (currentValueSubject == null)
        {
            return;
        }

        var fileValue = GetOrWriteValueFromFile();
        if (!currentValueSubject.Value.Item2.Equals(fileValue.Item2))
        {
            currentValueSubject?.OnNext(fileValue);
        }
    }

    private void WriteChangedValue()
    {
        currentValueSubject?.Value.Item1.Run(Save);
    }

    public class InvalidDatabasePath(string path, Exception exception) : Exception;
}

public class FileWatchingDatabaseFile<T> : AbstractDatabaseFile<T>
{
    public FileWatchingDatabaseFile(IFileAccess fileAccess, DatabaseBinder databaseBinder, string path,
        IDbOptions<T> dbOptions) : base(fileAccess, databaseBinder, path, dbOptions)
    {
        databaseBinder.BindToFile(fileAccess, path, Invalidate);
    }
}