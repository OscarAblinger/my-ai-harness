using Ablinger.MyAiHarness.Core.Harness.FileAccess;

namespace Ablinger.MyAiHarness.Core.Harness.Database;

/// <summary>
/// Provider class useful for usage with dependency injection libraries.
/// </summary>
public class DbProvider(IFileAccess fileAccess, DatabaseBinder databaseBinder)
{
    public DatabaseDirectory<T> CreateDbDirectory<T>(string path, IDbOptions<T> dbOptions,
        IChildPathTransformer? childPathTransformer = null, IFileAccess.FileWatchOptions? fileWatchOptions = null)
    {
        return new DatabaseDirectory<T>(fileAccess, databaseBinder, path, dbOptions, childPathTransformer,
            fileWatchOptions);
    }

    public FileWatchingDatabaseFile<T> CreateFileWatchingDbFile<T>(string path, IDbOptions<T> dbOptions)
    {
        return new FileWatchingDatabaseFile<T>(fileAccess, databaseBinder, path, dbOptions);
    }
}