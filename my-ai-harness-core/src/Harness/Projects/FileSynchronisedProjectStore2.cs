using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Conversations;
using Ablinger.MyAiHarness.Core.Harness.Database;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Harness.Shutdown;
using Ablinger.MyAiHarness.Core.Utils.Serialisation;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Projects;

public class FileSynchronisedProjectStore2
{
    public class FileSynchronisedProjectStoreProvider(
        IFileAccess fileAccess,
        DatabaseBinder databaseBinder,
        OnHarnessShutdownCallback onHarnessShutdownCallback)
    {
        public FileSynchronisedProjectStore2 createForPath(string projectsPath)
        {
            return new FileSynchronisedProjectStore2(projectsPath, fileAccess, databaseBinder,
                onHarnessShutdownCallback);
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };

    public IObservableList<IObservable<Project>> Projects { get; init; }

    private readonly DatabaseDirectory<Project> dbDirectory;
    private readonly string projectsPath;
    private readonly IFileAccess fileAccess;
    private readonly CompositeDisposable compositeDisposable = new();

    private FileSynchronisedProjectStore2(string projectsPath, IFileAccess fileAccess,
        DatabaseBinder databaseBinder, OnHarnessShutdownCallback onHarnessShutdownCallback)
    {
        this.projectsPath = projectsPath;
        this.fileAccess = fileAccess;

        onHarnessShutdownCallback.OnShutdown += (_, _) => compositeDisposable.Dispose();

        dbDirectory = new DatabaseDirectory<Project>(fileAccess, databaseBinder, projectsPath,
            path => new Project()
            {
                Name = Path.GetFileNameWithoutExtension(projectsPath),
                Path = path,
                Global = true
            },
            ProjectPathFileTransformer,
            new IFileAccess.FileWatchOptions([], true));
        compositeDisposable.Add(dbDirectory);

        Projects = dbDirectory.AsObservable
            .Connect()
            .Transform(dbFile => dbFile.AsObservable)
            .AsObservableList();
    }

    /// <summary>
    /// Creates a new project and ensures its synchronised to the file system.
    /// </summary>
    /// <param name="name">The name of the project. Note that they have to be unique.</param>
    /// <returns>The new project.</returns>
    public Project CreateNewProject(string name)
    {
        var value = new Project
        {
            Name = name,
            Path = PathMaker.FindUniquePath(fileAccess, projectsPath, name),
            Global = true
        };
        dbDirectory.Create(name, value);
        return value;
    }

    /// <summary>
    /// Deletes the project and all of its files from the file system.
    /// This actual should usually require confirmation in the UI as its destructive and cannot be undone.
    /// </summary>
    /// <param name="project">The project to delete.</param>
    public void DeleteProject(Project project)
    {
        dbDirectory.Delete(project);
    }

    private static IChildPathTransformer ProjectPathFileTransformer = new ProjectPathFileTransformerImpl();

    private sealed class ProjectPathFileTransformerImpl : IChildPathTransformer
    {
        public string? TransformFromChildPath(string dirpath)
        {
            return Path.Combine(dirpath, FileConstants.ProjectFileName);
        }

        public string TransformToChildPath(string filePath)
        {
            if (Path.GetFileName(filePath) != FileConstants.ProjectFileName)
                throw new ArgumentException($"Given filePath '${filePath}' is not actually a valid project file path.");

            return Path.GetDirectoryName(filePath) ??
                   throw new ArgumentException(
                       $"Given filePath '${filePath}' is not actually a valid project file path.");
        }
    }
}