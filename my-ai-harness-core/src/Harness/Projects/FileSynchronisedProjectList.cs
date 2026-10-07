using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Conversations;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Utils;
using DynamicData;
using DynamicData.Alias;

namespace Ablinger.MyAiHarness.Core.Harness.Projects;

public class FileSynchronisedProjectList
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };

    internal FileSynchronisedProjectList(string projectsPath, IFileAccess fileAccess)
    {
        this.projectsPath = projectsPath;
        this.fileAccess = fileAccess;

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

        // Whenever projects or any of its descendants are changed, we want to reserialise/delete the project
        Projects
            // Projects
            .OnItemAdded(Reserialise)
            .OnItemRemoved(Delete)
            // Conversations
            .TransformMany(p => p.Conversations.Items)
            .OnItemAdded(conv => Reserialise(conv.Project))
            .OnItemRemoved(conv => Reserialise(conv.Project))
            // ConversationPoints
            .TransformMany(conv => conv.ConversationPoints.Items)
            .OnItemAdded(cp => Reserialise(cp.Conversation.Project))
            .OnItemRemoved(cp => Reserialise(cp.Conversation.Project));
    }

    public IObservable<IChangeSet<Project>> Projects { get; }

    private readonly SourceList<Project> projects;
    private readonly string projectsPath;
    private readonly IFileAccess fileAccess;

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
            fileAccess.CreateDirectory(projectToSerialise.Path);
            var projectJson = JsonSerializer.Serialize(projectToSerialise, JsonSerializerOptions);
            fileAccess.WriteAllText(Path.Combine(projectToSerialise.Path, FileConstants.ProjectFileName), projectJson);
        }
    }

    private void Delete(Project project)
    {
        lock (projects)
        {
            fileAccess.DeleteDirectory(project.Path, true);
        }
    }
}