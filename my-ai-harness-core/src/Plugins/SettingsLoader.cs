using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.Json;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;

namespace Ablinger.MyAiHarness.Core.Plugins;

public class SettingsLoader
{
    private readonly MahSettings?[] settingsLayers = new MahSettings?[2];
    private readonly string?[] settingsFiles = new string?[2];

    private const int LayerGlobal = 1;
    private const int LayerProject = 0;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true
    };

    public void LoadGlobalSettings(string settingsFile, bool createIfNotExist = true)
    {
        settingsFiles[LayerGlobal] = settingsFile;
        settingsLayers[LayerGlobal] = LoadSettingsLayer(settingsFile, createIfNotExist);
    }

    public void LoadProjectSettings(string settingsFile, bool createIfNotExist = true)
    {
        settingsFiles[LayerProject] = settingsFile;
        settingsLayers[LayerProject] = LoadSettingsLayer(settingsFile, createIfNotExist);
    }

    private MahSettings? LoadSettingsLayer(string settingsFile, bool createIfNotExist = true)
    {
        try
        {
            return JsonSerializer.Deserialize<MahSettings>(File.ReadAllText(settingsFile), JsonSerializerOptions);
        }
        catch (Exception e)
            when (e is FileNotFoundException or SecurityException or IOException)
        {
            if (!createIfNotExist)
            {
                return null;
            }

            MahSettings defaultSettings = new();
            Directory.CreateDirectory(new FileInfo(settingsFile).Directory!.FullName);
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(defaultSettings, JsonSerializerOptions));
            return defaultSettings;
        }
    }

    public void SaveGlobalSettings(params ISettings[] changedSettings)
    {
        SaveSettings(changedSettings, LayerGlobal);
    }

    public void SaveProjectSettings(params ISettings[] changedSettings)
    {
        SaveSettings(changedSettings, LayerProject);
    }

    private void SaveSettings(ISettings[] changedSettings, int layer)
    {
        var settingsLayer = settingsLayers[layer] ??= new MahSettings();

        foreach (var changedSetting in changedSettings)
        {
            var key = changedSetting.GetType().AssemblyQualifiedName!;
            settingsLayer.PluginSettings[key] =
                JsonSerializer.SerializeToElement(changedSetting, changedSetting.GetType(), JsonSerializerOptions);
        }

        var settingsFile = settingsFiles[layer]!;
        Directory.CreateDirectory(new FileInfo(settingsFile).Directory!.FullName);
        File.WriteAllText(settingsFile, JsonSerializer.Serialize(settingsLayer, JsonSerializerOptions));
    }

    public T LoadSettings<T>()
        where T : ISettings, new()
    {
        return LoadSettings(() => new T());
    }

    public T LoadSettings<T>(Func<T> defaultInitializer)
        where T : ISettings
    {
        var type = typeof(T);
        string settingsKey = type.AssemblyQualifiedName!;

        foreach (var settingsLayer in settingsLayers)
        {
            if (settingsLayer is { } ms && ms.PluginSettings.TryGetValue(settingsKey, out var jsonElement))
            {
                var settings = jsonElement.Deserialize<T>();
                if (settings != null)
                {
                    return settings;
                }
            }
        }

        return defaultInitializer();
    }

    /// <summary>
    /// The object containing all settings.
    /// </summary>
    private struct MahSettings
    {
        public Dictionary<string, JsonElement> PluginSettings
        {
            get { return field ??= new Dictionary<string, JsonElement>(); }
            init;
        }
    }
}