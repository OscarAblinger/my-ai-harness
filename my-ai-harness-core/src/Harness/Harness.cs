using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Harness.Projects;
using Ablinger.MyAiHarness.Core.Harness.Prompting;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;
using Ablinger.MyAiHarness.Core.Plugins;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;

namespace Ablinger.MyAiHarness.Core.Harness;

public class Harness
{
    public static void AddHarnessServices(ServiceCollection services)
    {
        services.AddSingleton<PromptProcessor>();
        services.AddSingleton<ILLMSource, OpenAILikeLLMSource>();
        services.AddSingleton<IOpenAILikeLLMSource, OpenAILikeLLMSource>();
    }

    private readonly ServiceProvider serviceProvider;

    public Harness(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        
        var pluginLoader = serviceProvider.GetRequiredService<PluginLoader>();
        var plugins = pluginLoader.LoadAllPluginProviders(serviceProvider)
            .Select(pl => pl.GetPlugin())
            .ToList();

        var settingsLoader = serviceProvider.GetRequiredService<SettingsLoader>();
        var fileAccess = serviceProvider.GetRequiredService<IFileAccess>();
        InitialiseProjects(settingsLoader, fileAccess);
    }

    private void InitialiseProjects(SettingsLoader settingsLoader, IFileAccess fileAccess)
    {
        var generalSettings = settingsLoader.LoadSettings<GeneralSettings>();
        var projectsPath = Path.Combine(generalSettings.GlobalPath, FileConstants.ProjectDir);
        Projects = new FileSynchronisedProjectList(projectsPath, fileAccess);
    }

    public void Start()
    {
        List<Task> tasks = new();
        foreach (var prompter in serviceProvider.GetServices<IPrompter>())
        {
            tasks.Add(prompter.Register(this));
        }

        foreach (var task in tasks)
        {
            task.Wait();
        }
    }

    public FileSynchronisedProjectList Projects { get; private set; }
}