using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;

namespace Ablinger.MyAiHarness.Core.Harness.FileAccess;

public interface IFileAccess
{
    /// <inheritdoc cref="File.ReadAllText(string)"/>
    string ReadAllText(string path);

    /// <inheritdoc cref="File.WriteAllText(string, string)"/>
    void WriteAllText(string path, string contents);

    /// <inheritdoc cref="Directory.CreateDirectory(string)"/>
    void CreateDirectory(string path);

    /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
    IEnumerable<string> EnumerateDirectories(string path);

    /// <inheritdoc cref="Directory.EnumerateFiles(string)"/>
    IEnumerable<string> EnumerateFiles(string projectPath);

    /// <inheritdoc cref="Directory.Delete(string, bool)"/>
    void DeleteDirectory(string directoryPath, bool recursive);

    /// <inheritdoc cref="File.Delete(string)"/>
    void DeleteFile(string filePath);

    /// <summary>
    /// Returns true if the given path refers to an existing file OR directory.
    /// </summary>
    bool Exists(string path);

    /// <inheritdoc cref="File.OpenRead(string)"/>
    Stream OpenRead(string filePath);

    /// <summary>
    /// Returns an observable of file system events, which are sent when the file is created, updated, renamed, or deleted.
    /// </summary>
    /// <param name="path">The path to the file or directory to watch.</param>
    /// <param name="opts">Options to further specify the watch behaviour.</param>
    /// <returns>An observable that updates as the contents do.</returns>
    IObservable<EventPattern<FileSystemEventArgs>> WatchFileChanges(string path, FileWatchOptions? opts = null);

    public record struct FileWatchOptions(ICollection<string> Filters, bool IncludeSubdirectories)
    {
        public FileWatchOptions() : this([], false) {}
        
        public FileWatchOptions(params List<string> filters) : this(filters, false) {}
    }
}