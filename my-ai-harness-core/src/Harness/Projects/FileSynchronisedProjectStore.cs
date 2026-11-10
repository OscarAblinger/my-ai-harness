using System;
using System.IO;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Conversations;
using Ablinger.MyAiHarness.Core.Harness.Database;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Harness.Shutdown;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Projects;

public class FileSynchronisedProjectStore
{
    public class FileSynchronisedProjectStoreProvider(
        IFileAccess fileAccess,
        DbProvider dbProvider,
        OnHarnessShutdownCallback onHarnessShutdownCallback)
    {
        public FileSynchronisedProjectStore CreateForPath(string projectsPath)
        {
            return new FileSynchronisedProjectStore(projectsPath, fileAccess, dbProvider,
                onHarnessShutdownCallback);
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };

    public IObservableList<IObservable<Project>> Projects { get; init; }
    
    private readonly CompositeDisposable compositeDisposable = new();
    private readonly string projectsPath;
    private readonly IFileAccess fileAccess;
    private readonly DbProvider dbProvider;
    private readonly DatabaseDirectory<Project> dbDirectory;

    private FileSynchronisedProjectStore(string projectsPath, IFileAccess fileAccess,
        DbProvider dbProvider, OnHarnessShutdownCallback onHarnessShutdownCallback)
    {
        this.projectsPath = projectsPath;
        this.fileAccess = fileAccess;
        this.dbProvider = dbProvider;

        onHarnessShutdownCallback.OnShutdown += (_, _) => compositeDisposable.Dispose();

        IDbOptions<Project> dbOptions = new LambdaBasedDbOption<Project>(
            path => new Project()
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
                Global = true
            },
            BindAndInitialiseConversations
        );

        dbDirectory = dbProvider.CreateDbDirectory(projectsPath, dbOptions, ProjectPathFileTransformer,
            new IFileAccess.FileWatchOptions([], true));
        compositeDisposable.Add(dbDirectory);

        Projects = dbDirectory.AsObservable
            .Connect()
            .Transform(dbFile => dbFile.AsObservable)
            .AsObservableList();
    }

    private Project BindAndInitialiseConversations(Project project)
    {
        IDbOptions<Conversation> conversationDbOptions = new LambdaBasedDbOption<Conversation>(
            path => new Conversation
            {
                Project = project,
                Name = "New conversation",
                ConversationPoints = new SourceList<ConversationPoint>()
            },
            conversation => conversation
        );

        var conversationDirectory = dbProvider.CreateDbDirectory(
            Path.Combine(project.Path, FileConstants.ProjectConversationsDir),
            conversationDbOptions
        );

        return new Project
        {
            Name = project.Name,
            Path = project.Path,
            Global = project.Global,
            Conversations = conversationDirectory.AsObservable
                .Connect()
                .Transform(dbFile => dbFile.AsObservable)
                .AsObservableList()
        };
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