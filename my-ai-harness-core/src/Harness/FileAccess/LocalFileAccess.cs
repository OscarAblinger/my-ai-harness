using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.FileAccess;

public class LocalFileAccess : IFileAccess
{
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        return Directory.EnumerateDirectories(path);
    }

    public IEnumerable<string> EnumerateFiles(string projectPath)
    {
        return Directory.EnumerateFiles(projectPath);
    }

    public void DeleteDirectory(string directoryPath, bool recursive)
    {
        Directory.Delete(directoryPath, recursive);
    }

    public void DeleteFile(string filePath)
    {
        File.Delete(filePath);
    }

    public bool Exists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }

    public Stream OpenRead(string filePath)
    {
        return File.OpenRead(filePath);
    }

    public IObservable<EventPattern<FileSystemEventArgs>> WatchFileChanges(string path, IFileAccess.FileWatchOptions? opts = null)
    {
        var fileSystemWatcher = new FileSystemWatcher(path);
        
        opts ??= new IFileAccess.FileWatchOptions();
        fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;
        fileSystemWatcher.Filters.Add(opts.Value.Filters);
        fileSystemWatcher.IncludeSubdirectories = opts.Value.IncludeSubdirectories;

        return Observable.FromEventPattern<FileSystemEventArgs>(
            h =>
            {
                fileSystemWatcher.Created += h.Invoke;
                fileSystemWatcher.Changed += h.Invoke;
                fileSystemWatcher.Deleted += h.Invoke;
                fileSystemWatcher.Renamed += h.Invoke;
            },
            h =>
            {
                fileSystemWatcher.Created -= h.Invoke;
                fileSystemWatcher.Changed -= h.Invoke;
                fileSystemWatcher.Deleted -= h.Invoke;
                fileSystemWatcher.Renamed -= h.Invoke;
            });
    }
}