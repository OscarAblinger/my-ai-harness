using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ablinger.MyAiHarness.Core.Harness.Prompting;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;
using Ablinger.MyAiHarness.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Ablinger.MyAiHarness.Core.Harness;

public class Harness(ServiceProvider serviceProvider)
{

    public static void AddHarnessServices(ServiceCollection services)
    {
        services.AddSingleton<PromptProcessor>();
        services.AddSingleton<ILLMSource, OpenAILikeLLMSource>();
        services.AddSingleton<IOpenAILikeLLMSource, OpenAILikeLLMSource>();
    }
    
    public void Start()
    {
        var pluginLoader = serviceProvider.GetRequiredService<PluginLoader>();
        var plugins = pluginLoader.LoadAllPluginProviders(serviceProvider)
            .Select(pl => pl.GetPlugin())
            .ToList();

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
}