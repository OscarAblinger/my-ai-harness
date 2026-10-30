using System;
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

public class DatabaseFile<T> : IDisposable
    where T : ObservableObject
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };

    private CompositeDisposable disposables = new();
    private readonly IFileAccess fileAccess;
    private readonly DatabaseBinder databaseBinder;
    private readonly string path;
    private readonly Func<T> defaultCreator;

    private BehaviorSubject<(T, JsonNode)>? currentValueSubject;

    public DatabaseFile(IFileAccess fileAccess, DatabaseBinder databaseBinder, string path, Func<T> defaultCreator)
    {
        this.fileAccess = fileAccess;
        this.databaseBinder = databaseBinder;
        this.path = path;
        this.defaultCreator = defaultCreator;

        databaseBinder.BindToFile<T>(fileAccess, path, Invalidate);
    }

    public IObservable<T> AsObservable => Observable.Defer(GetCurrentValueSubject).Select(tuple => tuple.Item1);

    private BehaviorSubject<(T, JsonNode)> GetCurrentValueSubject()
    {
        currentValueSubject ??= new(GetOrWriteValueFromFile());
        return currentValueSubject;
    }

    public void Dispose()
    {
        currentValueSubject?.Dispose();
        disposables.Dispose();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="DatabaseFile{T}.InvalidDatabasePath">The given path is not valid.</exception>
    /// <exception cref="IOException">An I/O error occurred while opening the file.</exception>
    /// <exception cref="NotSupportedException">If type `T` cannot be serialised or deserialised.</exception>
    private (T, JsonNode) GetOrWriteValueFromFile()
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
                        disposables.Add(databaseBinder.BindObject(deserializedValue, WriteChangedValue));

                        return (deserializedValue, deserializedNode);
                    }
                }
            }
            catch (JsonException exception)
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
            // `value` is `null` here so we will create the file in the next `if`
        }

        var value = defaultCreator();
        disposables.Add(databaseBinder.BindObject(value, WriteChangedValue));
        Save(value);
        return (value, JsonSerializer.SerializeToNode(value, JsonSerializerOptions)!);
    }

    private void Save(T value)
    {
        Path.GetDirectoryName(path)?.Run(fileAccess.CreateDirectory);
        var serializedValue = JsonSerializer.Serialize(value, JsonSerializerOptions);
        fileAccess.WriteAllText(path, serializedValue);
    }

    private void Invalidate()
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