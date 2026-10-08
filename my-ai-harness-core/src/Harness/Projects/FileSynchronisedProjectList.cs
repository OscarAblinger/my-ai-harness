using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Conversations;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Harness.Shutdown;
using Ablinger.MyAiHarness.Core.Utils.Serialisation;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Projects;

public class FileSynchronisedProjectList
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };

    internal FileSynchronisedProjectList(string projectsPath, IFileAccess fileAccess,
        OnHarnessShutdownCallback onHarnessShutdownCallback)
    {
        this.projectsPath = projectsPath;
        this.fileAccess = fileAccess;
        this.onHarnessShutdownCallback = onHarnessShutdownCallback;

        projects = new SourceList<Project>();
        foreach (var projectDir in fileAccess.EnumerateDirectories(projectsPath))
        {
            var project =
                JsonSerializer.Deserialize<Project>(
                    File.OpenRead(Path.Combine(projectDir, FileConstants.ProjectFileName)),
                    JsonSerializerOptions);
            if (project != null)
            {
                projects.Add(project);
            }
        }

        Projects = projects.Connect();

        var reserialiseBuffer = new Subject<Conversation>();
        var reserialiseBufferSubscription = reserialiseBuffer
            .Buffer(TimeSpan.FromMilliseconds(100))
            .SelectMany(batch => batch.Distinct())
            .Subscribe(ReserialiseConversation);

        bool shouldWrite = false;
        // Whenever projects or any of its descendants are changed, we want to reserialise/delete the project
        Action<Conversation> addToBuffer = p =>
        {
            // ReSharper disable once AccessToModifiedClosure
            if (!shouldWrite) return;
            reserialiseBuffer.OnNext(p);
        };
        var projectChangeSubscription = Projects
            // Projects
            .OnItemAdded(Reserialise)
            .OnItemRemoved(Delete, invokeOnUnsubscribe: false)
            // Conversations
            .TransformMany(p => p.Conversations)
            .OnItemAdded(conv => addToBuffer(conv))
            .OnItemRemoved(conv => DeleteConversation(conv), invokeOnUnsubscribe: false)
            // ConversationPoints
            .TransformMany(conv => conv.ConversationPoints)
            .OnItemAdded(cp => addToBuffer(cp.Conversation))
            .OnItemRemoved(cp => addToBuffer(cp.Conversation), invokeOnUnsubscribe: false)
            // Subscribe so the listeners actually get executed
            .Subscribe();

        // activating the Write only after subscribing so that the initial loading of all projects does not trigger
        // serialisation
        shouldWrite = true;

        onHarnessShutdownCallback.OnShutdown += (sender, args) =>
        {
            reserialiseBufferSubscription.Dispose();
            projectChangeSubscription.Dispose();
        };
    }

    public IObservable<IChangeSet<Project>> Projects { get; }

    private readonly SourceList<Project> projects;
    private readonly string projectsPath;
    private readonly IFileAccess fileAccess;
    private readonly OnHarnessShutdownCallback onHarnessShutdownCallback;

    /// <summary>
    /// Creates a new project and ensures its synchronised to the file system.
    /// </summary>
    /// <param name="name">The name of the project. Note that they have to be unique.</param>
    /// <returns>The new project.</returns>
    public Project CreateNewProject(string name)
    {
        lock (projects)
        {
            if (projects.Items.Any(p => p.Name == name))
            {
                throw new ArgumentException($"Project with the name ${name} already exists.");
            }

            var project = new Project
            {
                Name = name,
                // TODO: ensure file system compatible name
                Path = Path.Combine(projectsPath, name),
                Global = true
            };
            // Adding it automatically creates and serialises the project
            projects.Add(project);
            return project;
        }
    }

    /// <summary>
    /// Deletes the project and all of its files from the file system.
    /// This actual should usually require confirmation in the UI as its destructive and cannot be undone.
    /// </summary>
    /// <param name="project">The project to delete.</param>
    public void DeleteProject(Project project)
    {
        lock (projects)
        {
            // will automatically delete file contents via observers
            projects.Remove(project);
        }
    }

    private void Reserialise(Project projectToSerialise)
    {
        lock (projects)
        {
            if (!projects.Items.Contains(projectToSerialise))
            {
                return;
            }

            try
            {
                fileAccess.CreateDirectory(projectToSerialise.Path);
                var projectJson = JsonSerializer.Serialize(projectToSerialise, JsonSerializerOptions);
                fileAccess.WriteAllText(Path.Combine(projectToSerialise.Path, FileConstants.ProjectFileName),
                    projectJson);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to serialise project ${projectToSerialise}", ex);
            }
        }
    }

    private void Delete(Project project)
    {
        lock (projects)
        {
            try
            {
                fileAccess.DeleteDirectory(project.Path, true);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to delete project ${project}", ex);
            }
        }
    }

    private void ReserialiseConversation(Conversation conversation)
    {
        lock (projects)
        {
            if (!projects.Items.Contains(conversation.Project))
            {
                return;
            }

            try
            {
                fileAccess.CreateDirectory(Path.Combine(conversation.Project.Path,
                    FileConstants.ProjectConversationsDir));
                var serializerOptions = new JsonSerializerOptions(JsonSerializerOptions);
                serializerOptions.Converters.Add(new ExistingElementStorageConverter([conversation.Project]));
                var projectJson = JsonSerializer.Serialize(conversation, JsonSerializerOptions);
                // TODO: ensure file system compatible name
                fileAccess.WriteAllText(Path.Combine(conversation.Project.Path, conversation.Name), projectJson);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to serialise conversation ${conversation}", ex);
            }
        }
    }

    private void DeleteConversation(Conversation conversation)
    {
        lock (projects)
        {
            try
            {
                // TODO: ensure file system compatible name
                fileAccess.DeleteFile(Path.Combine(conversation.Project.Path, conversation.Name));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to delete conversation ${conversation}", ex);
            }
        }
    }
}