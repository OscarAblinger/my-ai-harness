using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ablinger.MyAiHarness.Core.Harness.Database;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Harness.Projects;
using Ablinger.MyAiHarness.Core.Harness.Prompting;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;
using Ablinger.MyAiHarness.Core.Harness.Shutdown;
using Ablinger.MyAiHarness.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Ablinger.MyAiHarness.Core.Harness;

public class Harness
{
    public static void AddHarnessServices(ServiceCollection services)
    {
        services.AddSingleton<PromptProcessor>();
        services.AddSingleton<ILLMSource, OpenAILikeLLMSource>();
        services.AddSingleton<IOpenAILikeLLMSource, OpenAILikeLLMSource>();
        services.AddSingleton<OnHarnessShutdownCallback>();
        services.AddSingleton<DatabaseBinder>();
    }

    private readonly ServiceProvider serviceProvider;

    public Harness(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        
        var pluginLoader = serviceProvider.GetRequiredService<PluginLoader>();
        // load plugins
        var plugins = pluginLoader.LoadAllPluginProviders(serviceProvider)
            .Select(pl => pl.GetPlugin())
            .ToList();

        var settingsLoader = serviceProvider.GetRequiredService<SettingsLoader>();
        var fileAccess = serviceProvider.GetRequiredService<IFileAccess>();
        Projects = InitialiseProjects(settingsLoader, fileAccess);
    }

    private FileSynchronisedProjectStore InitialiseProjects(SettingsLoader settingsLoader, IFileAccess fileAccess)
    {
        var generalSettings = settingsLoader.LoadSettings<GeneralSettings>();
        var projectsPath = Path.Combine(generalSettings.GlobalPath, FileConstants.ProjectDir);
        return new FileSynchronisedProjectStore(projectsPath, fileAccess, serviceProvider.GetRequiredService<OnHarnessShutdownCallback>());
    }

    public void Start()
    {
        List<Task> tasks = [];
        foreach (var prompter in serviceProvider.GetServices<IPrompter>())
        {
            tasks.Add(prompter.Register(this));
        }

        foreach (var task in tasks)
        {
            task.Wait();
        }
    }

    public void ShutdownCleanly(CancellationToken? cancellationToken = null)
    {
        serviceProvider.GetRequiredService<OnHarnessShutdownCallback>().SignalShutdown(
            new ShutdownHandlerArgs(cancellationToken ?? CancellationToken.None));
    }

    public FileSynchronisedProjectStore Projects { get; private set; }
}