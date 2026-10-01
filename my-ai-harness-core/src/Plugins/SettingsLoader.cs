using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text.Json;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;
using Ablinger.MyAiHarness.Core.Utils;

namespace Ablinger.MyAiHarness.Core.Plugins;

public class SettingsLoader
{
    private readonly MahSettings?[] settingsLayers = new MahSettings?[2];
    private readonly string?[] settingsFiles = new string?[2];
    private readonly IFileAccess file;

    public SettingsLoader(IFileAccess file)
    {
        this.file = file;
    }

    private const int LayerGlobal = 1;
    private const int LayerProject = 0;

    private static string SettingsKey<T>() => SettingsKey(typeof(T));
    private static string SettingsKey(Type t) => t.FullName!;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true
    };

    private IAllSettings? allSettingsField;

    public IAllSettings AllSettings
    {
        get { return allSettingsField ??= new AllSettingsLoader(settingsLayers); }
    }

    public void LoadGlobalSettings(string settingsFile, bool createIfNotExist = true)
    {
        settingsFiles[LayerGlobal] = settingsFile;
        settingsLayers[LayerGlobal] = LoadSettingsLayer(settingsFile, createIfNotExist);
        allSettingsField = null;
    }

    public void LoadProjectSettings(string settingsFile, bool createIfNotExist = true)
    {
        settingsFiles[LayerProject] = settingsFile;
        settingsLayers[LayerProject] = LoadSettingsLayer(settingsFile, createIfNotExist);
        allSettingsField = null;
    }

    private MahSettings? LoadSettingsLayer(string settingsFile, bool createIfNotExist = true)
    {
        try
        {
            return JsonSerializer.Deserialize<MahSettings>(file.ReadAllText(settingsFile), JsonSerializerOptions);
        }
        catch (Exception e)
            when (e is FileNotFoundException or SecurityException or IOException)
        {
            if (!createIfNotExist)
            {
                return null;
            }

            MahSettings defaultSettings = new();
            file.CreateDirectory(new FileInfo(settingsFile).Directory!.FullName);
            file.WriteAllText(settingsFile, JsonSerializer.Serialize(defaultSettings, JsonSerializerOptions));
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
            var key = SettingsKey(changedSetting.GetType());
            settingsLayer.PluginSettings[key] =
                JsonSerializer.SerializeToElement(changedSetting, changedSetting.GetType(), JsonSerializerOptions);
        }

        allSettingsField = null;
        var settingsFile = settingsFiles[layer]!;
        file.CreateDirectory(new FileInfo(settingsFile).Directory!.FullName);
        file.WriteAllText(settingsFile, JsonSerializer.Serialize(settingsLayer, JsonSerializerOptions));
    }

    public T LoadSettings<T>()
        where T : ISettings, new()
    {
        return LoadSettings(() => new T());
    }

    public T LoadSettings<T>(Func<T> defaultInitializer)
        where T : ISettings
    {
        var settingsKey = SettingsKey<T>();

        foreach (var settingsLayer in settingsLayers)
        {
            if (settingsLayer is { } ms && ms.PluginSettings.TryGetValue(settingsKey, out var jsonElement))
            {
                var settings = jsonElement.Deserialize<T>(JsonSerializerOptions);
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
        public IDictionary<string, JsonElement> PluginSettings
        {
            get { return field ??= new Dictionary<string, JsonElement>(); }
            init;
        }
    }

    public interface IAllSettings
    {
        List<GroupSetting> GroupSettings { get; }
    }

    public struct GroupSetting
    {
        public string Name { get; init; }

        /// <summary>
        /// If the plugin is not currently loaded, then the setting also cannot be loaded.
        /// </summary>
        public bool CouldBeLoaded { get; init; }

        public ImmutableList<GroupEntry> Entries { get; init; }
    }

    public struct GroupEntry
    {
        public ISettings.SettingsEntry? Global { get; init; }
        public ISettings.SettingsEntry? Project { get; init; }
    }

    private class AllSettingsLoader : IAllSettings
    {
        public AllSettingsLoader(MahSettings?[] mahSettings)
        {
            var global =
                mahSettings[LayerGlobal]?.PluginSettings ?? ImmutableDictionary.Create<string, JsonElement>();
            var project =
                mahSettings[LayerProject]?.PluginSettings ?? ImmutableDictionary.Create<string, JsonElement>();

            var keys = new HashSet<string>();
            keys.UnionWith(global.Keys);
            keys.UnionWith(project.Keys);

            GroupSettings =
                (from key in keys
                    let type = TryLoadType(key)
                    let entries = type?.Run(t => LoadEntries(t, key, global, project))
                    orderby type?.Run(SettingsKey)
                    select new GroupSetting()
                    {
                        Name = key,
                        CouldBeLoaded = type != null && entries != null,
                        Entries = entries ?? ImmutableList.Create<GroupEntry>()
                    }
                )
                .ToList();
        }

        private ImmutableList<GroupEntry>? LoadEntries(
            Type type,
            string key,
            IDictionary<string, JsonElement> global,
            IDictionary<string, JsonElement> project)
        {
            if (!typeof(ISettings).IsAssignableFrom(type))
            {
                // not an ISettings -> could not be loaded
                return null;
            }

            List<ISettings.SettingsEntry>? globalEntries = null;
            if (global.TryGetValue(key, out var globalEl))
            {
                if (globalEl.Deserialize(type, JsonSerializerOptions) is ISettings globalObj)
                {
                    globalEntries = globalObj.GetEntries();
                }
            }

            List<ISettings.SettingsEntry>? projectEntries = null;
            if (project.TryGetValue(key, out var projectEl))
            {
                if (projectEl.Deserialize(type, JsonSerializerOptions) is ISettings projectObj)
                {
                    projectEntries = projectObj.GetEntries();
                }
            }

            return MergeEntries(globalEntries, projectEntries);
        }

        private ImmutableList<GroupEntry> MergeEntries(List<ISettings.SettingsEntry>? globalEntries,
            List<ISettings.SettingsEntry>? projectEntries)
        {
            // first let's deal with null/empty lists
            switch (globalEntries, projectEntries)
            {
                case (null, null):
                    return ImmutableList.Create<GroupEntry>();
                case (null, _):
                    return [.. projectEntries.Select(pe => new GroupEntry() { Project = pe })];
                case (_, null):
                    return [.. globalEntries.Select(ge => new GroupEntry() { Global = ge })];
                case var (ge, _) when ge.Count == 0:
                    return [.. projectEntries.Select(pe => new GroupEntry() { Project = pe })];
                case var (_, pe) when pe.Count == 0:
                    return [.. globalEntries.Select(ge => new GroupEntry() { Global = ge })];
            }

            var globalEntryIds = globalEntries.Select(entry => entry.Id).ToHashSet();
            var positionGlobalEntries = 0;

            var mergedEntries = new List<GroupEntry>(Math.Max(globalEntries.Count, projectEntries.Count));
            var leftOverProjectEntries = new List<ISettings.SettingsEntry>(projectEntries);
            leftOverProjectEntries.Reverse();

            while (positionGlobalEntries < globalEntries.Count)
            {
                var ge = globalEntries[positionGlobalEntries];
                var pe = leftOverProjectEntries[^1];

                // case 1: both match (standard case)
                // most implementations of settings should fall only into this case, as they should also return entries
                // without values
                if (ge.Id == pe.Id)
                {
                    mergedEntries.Add(new GroupEntry() { Global = ge, Project = pe });
                    positionGlobalEntries++;
                    leftOverProjectEntries.RemoveAt(leftOverProjectEntries.Count - 1);
                    continue;
                }

                var matchingEntry = leftOverProjectEntries.FindLastIndex(se => se.Id == ge.Id);
                // case 2: global entry is not in project entry
                if (matchingEntry == -1)
                {
                    mergedEntries.Add(new GroupEntry() { Global = ge });
                    positionGlobalEntries++;
                    continue;
                }

                // case 3: global entry is in the project enty, but project has different entries before that
                // -> we need to check if these other projects are also in the global list
                // -> add different non-global entries first until the next one that isn't in the global list
                // -> then add normal entry
                var entriesToAdd = leftOverProjectEntries[(1+matchingEntry)..]
                    .Where(entry => !globalEntryIds.Contains(entry.Id))
                    .ToList();
                mergedEntries.AddRange(
                    entriesToAdd.AsEnumerable().Reverse().Select(entry => new GroupEntry()
                    {
                        Project = entry
                    }));
                mergedEntries.Add(new GroupEntry()
                {
                    Global = ge,
                    Project = leftOverProjectEntries[matchingEntry]
                });
                
                leftOverProjectEntries.RemoveAt(matchingEntry);
                foreach (var entry in entriesToAdd)
                {
                    leftOverProjectEntries.Remove(entry);
                }
                positionGlobalEntries++;
            }

            mergedEntries.AddRange(globalEntries[positionGlobalEntries..]
                .Select(entry => new GroupEntry() { Global = entry }));
            mergedEntries.AddRange(leftOverProjectEntries
                .Select(entry => new GroupEntry() { Project = entry }));

            return ImmutableList.CreateRange(mergedEntries);
        }

        private Type? TryLoadType(string typeFullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeFullName))
                .FirstOrDefault(type => type != null);
        }

        private static T Pop<T>(List<T> list)
        {
            var lastEl = list[^1];
            list.RemoveAt(list.Count - 1);
            return lastEl;
        }

        public List<GroupSetting> GroupSettings { get; }
    }
}