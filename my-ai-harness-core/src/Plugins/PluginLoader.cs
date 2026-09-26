using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;
using Ablinger.MyAiHarness.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Tomlyn;

namespace Ablinger.MyAiHarness.Core.Plugins;

public class PluginLoader
{
    private readonly ConcurrentDictionary<CanonicalPath, PluginLoaderInfo> loadedPlugins = new();

    public void LoadDirectory(string pluginDirectory, bool createIfNotExist = true)
    {
        LoadConfigFile(pluginDirectory, Path.Combine(pluginDirectory, ".plugin-loader.toml"), createIfNotExist);
    }

    public void LoadConfigFile(string pluginDirectory, string pluginConfig, bool createIfNotExist = true)
    {
        PluginFile? config;
        try
        {
            config = TomlSerializer.Deserialize<PluginFile>(File.OpenRead(pluginConfig));
        }
        catch (Exception ex)
        when(ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // file doesn't exist, so we initialise it with the default:
            config = PluginFile.Default;
            var defaultConfigString = TomlSerializer.Serialize(config);
            Directory.CreateDirectory(new FileInfo(pluginConfig).Directory!.FullName);
            File.WriteAllText(pluginConfig, defaultConfigString);
        }

        if (config != null)
        {
            Load(pluginDirectory, config);
        }
    }

    public void Load(string pluginDirectory, PluginFile pluginFile)
    {
        Parallel.ForEach(pluginFile.Plugins, pluginSettings =>
        {
            if (!pluginSettings.Enabled) return;

            var pluginPath = Path.Combine(pluginDirectory, pluginSettings.Name + ".dll");
            var pluginLoaderInfo = GetPluginLoaderInfo(pluginPath);
            pluginLoaderInfo.AssemblyLoadContext.LoadFromAssemblyPath(pluginPath);
        });
    }

    public IEnumerable<IPluginProvider> LoadAllPluginProviders(ServiceProvider serviceProvider)
    {
        return from pluginLoaderInfo in loadedPlugins.Values
            select pluginLoaderInfo.GetPluginProvider(serviceProvider);
    }

    private PluginLoaderInfo GetPluginLoaderInfo(string pluginPath)
    {
        return loadedPlugins.AddOrUpdate(
            ToCanonicalPath(pluginPath),
            cp => new PluginLoaderInfo(cp),
            (_, cp) =>
            {
                cp.AssemblyLoadContext.Unload();
                return cp;
            });
    }

    private static CanonicalPath ToCanonicalPath(string pluginPath)
    {
        return new CanonicalPath(Path.GetFullPath(pluginPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToUpperInvariant());
    }

    private record struct CanonicalPath(string Path);

    private record struct PluginLoaderInfo(CanonicalPath CanonicalPath)
    {
        public readonly AssemblyLoadContext AssemblyLoadContext = new AssemblyLoadContext(CanonicalPath.Path);
        public CanonicalPath CanonicalPath { get; } = CanonicalPath;

        private IPluginProvider? pluginProvider;

        public IPluginProvider? GetPluginProvider(ServiceProvider serviceProvider)
        {
            pluginProvider ??= ReflectionUtils.FindImplementation<IPluginProvider>(AssemblyLoadContext.Assemblies, serviceProvider);
            return pluginProvider;
        }
    }
}