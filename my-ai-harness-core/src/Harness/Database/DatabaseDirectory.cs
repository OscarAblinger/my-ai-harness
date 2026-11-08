using System;
using System.IO;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using DynamicData;
using DynamicData.Kernel;

namespace Ablinger.MyAiHarness.Core.Harness.Database;

public class DatabaseDirectory<T> : IDisposable
{
    private readonly IFileAccess fileAccess;
    private readonly DatabaseBinder databaseBinder;
    private readonly Func<string, T> defaultCreator;
    private readonly IChildPathTransformer childPathTransformer;
    private readonly SourceCache<DirectoryWatchedDatabaseFile, string> children;

    public DatabaseDirectory(IFileAccess fileAccess, DatabaseBinder databaseBinder, string path,
        Func<string, T> defaultCreator,
        IChildPathTransformer? childPathTransformer = null, IFileAccess.FileWatchOptions? fileWatchOptions = null)
    {
        this.fileAccess = fileAccess;
        this.databaseBinder = databaseBinder;
        this.defaultCreator = defaultCreator;
        this.childPathTransformer = childPathTransformer ?? IChildPathTransformer.NoTransformChildPathTransformer;

        children = new(dbf => dbf.Key);

        databaseBinder.BindToDirectory(fileAccess, path, changes =>
        {
            foreach (var (filePath, changeType) in changes)
            {
                switch (changeType)
                {
                    case WatcherChangeTypes.Created:
                        children.Edit(edit =>
                        {
                            edit.Lookup(filePath)
                                .IfHasValue(file => file.Invalidate())
                                .Else(() =>
                                {
                                    var selectedPath = this.childPathTransformer.TransformFromChildPath(filePath);
                                    if (selectedPath != null)
                                    {
                                        edit.AddOrUpdate(new DirectoryWatchedDatabaseFile(filePath,
                                            fileAccess, databaseBinder, selectedPath, defaultCreator));
                                    }
                                });
                        });

                        break;
                    case WatcherChangeTypes.Changed:
                        children.Lookup(filePath).IfHasValue(changedFile => changedFile.Invalidate());
                        break;
                    case WatcherChangeTypes.Deleted:
                    case WatcherChangeTypes.Renamed:
                        children.Edit(edit =>
                        {
                            edit.Lookup(filePath).IfHasValue(changedFile => changedFile.Dispose());
                            edit.Remove(filePath);
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }, fileWatchOptions);
    }

    public void Create(string relativePath, T value)
    {
        children.Edit(edit =>
        {
            var directoryWatchedDatabaseFile = new DirectoryWatchedDatabaseFile(
                childPathTransformer.TransformToChildPath(relativePath),
                fileAccess, databaseBinder, relativePath, defaultCreator);
            directoryWatchedDatabaseFile.SetValue(value);
            edit.AddOrUpdate(directoryWatchedDatabaseFile);
        });
    }

    public void Delete(T value)
    {
        children.Edit(edit =>
        {
            edit.Items
                .FirstOrOptional(dbFile => dbFile.HasValue(value))
                .IfHasValue(edit.Remove);
        });
    }

    public void Dispose()
    {
        children.Edit(edit =>
        {
            foreach (var directoryWatchedDatabaseFile in edit.Items)
            {
                directoryWatchedDatabaseFile.Dispose();
            }
        });
    }

    public IObservableList<IDatabaseFile<T>> AsObservable => children.Connect()
        .RemoveKey()
        .Transform(IDatabaseFile<T> (dbFile) => dbFile)
        .AsObservableList();

    private class DirectoryWatchedDatabaseFile(
        string key,
        IFileAccess fileAccess,
        DatabaseBinder databaseBinder,
        string path,
        Func<string, T> defaultCreator)
        : AbstractDatabaseFile<T>(fileAccess, databaseBinder, path, defaultCreator)
    {
        public new void Invalidate()
        {
            base.Invalidate();
        }

        internal new void SetValue(T value)
        {
            base.SetValue(value);
        }

        internal new bool HasValue(T value)
        {
            return base.HasValue(value);
        }

        internal readonly string Key = key;
    }
}

public interface IChildPathTransformer
{
    /// <summary>
    /// Returning <code>null</code> tells the DatabaseDirectory to ignore this entry and not map it.
    /// </summary>
    string? TransformFromChildPath(string childPath);

    string TransformToChildPath(string transformedPath);

    public static readonly IChildPathTransformer NoTransformChildPathTransformer = new NoTransformChildPathTransformerImpl();

    private class NoTransformChildPathTransformerImpl : IChildPathTransformer
    {
        public string TransformFromChildPath(string childPath)
        {
            return childPath;
        }

        public string TransformToChildPath(string transformedPath)
        {
            return transformedPath;
        }
    };
}