using System.Collections.Immutable;
using Tomlyn;

namespace Ablinger.MyAiHarness.Core.Plugins;

public class PluginFile
{
    public PluginFile(ImmutableList<PluginSettings> plugins)
    {
        Plugins = plugins;
    }

    public static readonly TomlSerializerOptions SerializerOptions = new()
    {
        DottedKeyHandling = TomlDottedKeyHandling.Expand
        // todo: MetadataStore
    };

    public static readonly PluginFile Default = new PluginFile(ImmutableList.Create<PluginSettings>());

    public ImmutableList<PluginSettings> Plugins { get; }

    public struct PluginSettings(string name, bool enabled, ImmutableDictionary<string, object> settings)
    {
        public string Name { get; } = name;
        public bool Enabled { get; } = enabled;
        public ImmutableDictionary<string, object> Settings { get; } = settings;
    }
}