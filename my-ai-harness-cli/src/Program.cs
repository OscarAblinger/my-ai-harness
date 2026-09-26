using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Reflection;
using Ablinger.MyAiHarness.Core.Harness;
using Ablinger.MyAiHarness.Core.Harness.Prompting;
using Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;
using Ablinger.MyAiHarness.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Ablinger.MyAiHarness.CLI;

public class Program
{
    public static int Main(string[] args)
    {
        Option<List<DirectoryInfo>> pluginDirectories = new("--plugins", "-p")
        {
            Description = "The path to the plugins folder.",
            Arity = ArgumentArity.OneOrMore
        };
        Option<FileInfo> globalDirectory = new("--global-folder", "-G")
        {
            Description = "The path to the global folder containing the settings plugins and settings file.",
            Arity = ArgumentArity.ExactlyOne
        };
        Option<FileInfo> settingsDirectory = new("--project-settings", "--settings", "-s")
        {
            Description = "The path to the project level settings file.",
            Arity = ArgumentArity.ExactlyOne
        };

        RootCommand rootCommand = new();
        rootCommand.Options.Add(pluginDirectories);
        rootCommand.Options.Add(globalDirectory);
        rootCommand.Options.Add(settingsDirectory);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.WriteLine(error);
            }

            return 1;
        }

        var globalDir = parseResult.GetValue(globalDirectory);
        var pluginLoader = CreatePluginLoader(globalDir, parseResult.GetValue(pluginDirectories));
        var settingsLoader = CreateSettingsLoader(globalDir, parseResult.GetValue(settingsDirectory));

        var services = new ServiceCollection();

        services.AddSingleton(pluginLoader);
        services.AddSingleton(settingsLoader);
        services.AddSingleton<IPrompter, ConsolePrompter>();

        Harness.AddHarnessServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var harness = new Harness(serviceProvider);
        harness.Start();

        return 0;
    }

    private static PluginLoader CreatePluginLoader(FileInfo? globalDir, List<DirectoryInfo>? pluginDirectories)
    {
        var pluginLoader = new PluginLoader();
        // always load global plugins
        var globalPath = Path.Combine(
            globalDir?.FullName ?? Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)!,
            ".mah-plugins");
        pluginLoader.LoadDirectory(globalPath);

        // load default project plugins location if no others are specified
        if (pluginDirectories == null || pluginDirectories.Count == 0)
        {
            var defaultProjectDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".mah-plugins");
            if (defaultProjectDirectory != globalPath)
            {
                pluginLoader.LoadDirectory(defaultProjectDirectory);
            }

            return pluginLoader;
        }

        foreach (var pluginDirectory in pluginDirectories)
        {
            pluginLoader.LoadDirectory(pluginDirectory.FullName);
        }

        return pluginLoader;
    }

    private static SettingsLoader CreateSettingsLoader(FileInfo? globalDir, FileInfo? settingsDirectory)
    {
        var settingsLoader = new SettingsLoader();

        settingsLoader.LoadGlobalSettings(Path.Combine(
            globalDir?.Name ?? Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)!,
            ".mahsettings.json"));
        settingsLoader.LoadProjectSettings(settingsDirectory?.Name ??
                                           Path.Combine(Directory.GetCurrentDirectory(), ".mahsettings.json"));

        return settingsLoader;
    }
}