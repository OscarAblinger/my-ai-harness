using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using Ablinger.MyAiHarness.Core.Plugins;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;
using Moq;
using Xunit;

namespace Ablinger.MyAiHarness.Core.Tests.Plugins;

public class SettingsLoaderTests
{
    private const string GlobalSettingsPath = @"C:\settings\global\settings.json";
    private const string ProjectSettingsPath = @"C:\settings\project\settings.json";

    private static string SettingsKey<T>() => typeof(T).FullName!;

    /// <summary>
    /// Builds the JSON content of a settings file as <see cref="SettingsLoader"/> expects it:
    /// a "PluginSettings" object keyed by the full type name of each settings instance.
    /// </summary>
    private static string BuildSettingsFileJson(params ISettings[] settings)
    {
        var pluginSettings = new Dictionary<string, object?>();
        foreach (var entry in settings)
        {
            pluginSettings[entry.GetType().FullName!] = entry;
        }

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["PluginSettings"] = pluginSettings
        });
    }

    private static Mock<IFileAccess> FileMockThrowingNotFound(string path)
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(path)).Throws(new FileNotFoundException("not found", path));
        return file;
    }

    // ------------------------------------------------------------------
    // LoadSettings<T> - reading layers
    // ------------------------------------------------------------------

    [Fact]
    public void LoadSettings_ProjectLayer_ReturnsValueFromProjectFile()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Returns(BuildSettingsFileJson(
                new TestPluginSettings { Name = "from-project", Count = 1 }));

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        var settings = loader.LoadSettings<TestPluginSettings>();

        Assert.Equal("from-project", settings.Name);
        Assert.Equal(1, settings.Count);
    }

    [Fact]
    public void LoadSettings_GlobalLayer_ReturnsValueFromGlobalFile()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(GlobalSettingsPath))
            .Returns(BuildSettingsFileJson(
                new TestPluginSettings { Name = "from-global", Count = 2 }));

        var loader = new SettingsLoader(file.Object);
        loader.LoadGlobalSettings(GlobalSettingsPath);

        var settings = loader.LoadSettings<TestPluginSettings>();

        Assert.Equal("from-global", settings.Name);
        Assert.Equal(2, settings.Count);
    }

    [Fact]
    public void LoadSettings_ProjectAndGlobalLayer_ProjectOverridesGlobal()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(GlobalSettingsPath))
            .Returns(BuildSettingsFileJson(new TestPluginSettings { Name = "global" }));
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Returns(BuildSettingsFileJson(new TestPluginSettings { Name = "project" }));

        var loader = new SettingsLoader(file.Object);
        loader.LoadGlobalSettings(GlobalSettingsPath);
        loader.LoadProjectSettings(ProjectSettingsPath);

        Assert.Equal("project", loader.LoadSettings<TestPluginSettings>().Name);
    }

    [Fact]
    public void LoadSettings_KeyMissingInProjectLayer_FallsBackToGlobalLayer()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(GlobalSettingsPath))
            .Returns(BuildSettingsFileJson(new TestPluginSettings { Name = "global" }));
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Returns(BuildSettingsFileJson(new OtherPluginSettings()));

        var loader = new SettingsLoader(file.Object);
        loader.LoadGlobalSettings(GlobalSettingsPath);
        loader.LoadProjectSettings(ProjectSettingsPath);

        Assert.Equal("global", loader.LoadSettings<TestPluginSettings>().Name);
    }

    [Fact]
    public void LoadSettings_NoSettingsLoaded_ReturnsNewDefaultInstance()
    {
        var loader = new SettingsLoader(new Mock<IFileAccess>().Object);

        var settings = loader.LoadSettings<TestPluginSettings>();

        Assert.Equal(string.Empty, settings.Name);
        Assert.Equal(0, settings.Count);
    }

    [Fact]
    public void LoadSettings_FileWithoutMatchingKey_ReturnsDefaultInstance()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Returns(BuildSettingsFileJson(new OtherPluginSettings()));

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        Assert.Equal(string.Empty, loader.LoadSettings<TestPluginSettings>().Name);
    }

    [Fact]
    public void LoadSettings_JsonValueIsNull_ReturnsDefaultInstance()
    {
        var json = $$"""
                     {
                         "PluginSettings": {
                             "{{SettingsKey<TestPluginSettings>()}}": null
                         }
                     }
                     """;
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(json);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        Assert.Equal(string.Empty, loader.LoadSettings<TestPluginSettings>().Name);
    }

    [Fact]
    public void LoadSettings_CustomDefaultInitializer_UsedWhenNoValueFound()
    {
        var loader = new SettingsLoader(new Mock<IFileAccess>().Object);

        var settings = loader.LoadSettings(() => new TestPluginSettings { Name = "custom-default", Count = 99 });

        Assert.Equal("custom-default", settings.Name);
        Assert.Equal(99, settings.Count);
    }

    [Fact]
    public void LoadSettings_AllowsTrailingCommas_InFileAndInIndividualSettingValues()
    {
        var json = $$"""
                     {
                         "PluginSettings": {
                             "{{SettingsKey<TestPluginSettings>()}}": {
                                 "Name": "trailing-comma",
                                 "Count": 3,
                             },
                         },
                     }
                     """;
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(json);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        var settings = loader.LoadSettings<TestPluginSettings>();
        Assert.Equal("trailing-comma", settings.Name);
        Assert.Equal(3, settings.Count);
    }

    [Fact]
    public void LoadSettings_AllowsTrailingCommas_BetweenPluginEntries()
    {
        var json = $$"""
                     {
                         "PluginSettings": {
                             "{{SettingsKey<TestPluginSettings>()}}": {
                                 "Name": "first",
                                 "Count": 1
                             },
                             "{{SettingsKey<OtherPluginSettings>()}}": {
                                 "Tag": "second"
                             },
                         },
                     }
                     """;
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(json);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        Assert.Equal("first", loader.LoadSettings<TestPluginSettings>().Name);
        Assert.Equal("second", loader.LoadSettings<OtherPluginSettings>().Tag);
    }

    // ------------------------------------------------------------------
    // Missing / unreadable files
    // ------------------------------------------------------------------

    [Fact]
    public void LoadProjectSettings_FileNotFound_CreatesDirectoryAndWritesDefaultFile()
    {
        var file = FileMockThrowingNotFound(ProjectSettingsPath);
        string? writtenContent = null;
        file.Setup(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, contents) => writtenContent = contents);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        file.Verify(f => f.CreateDirectory(@"C:\settings\project"), Times.Once());
        file.Verify(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()), Times.Once());

        Assert.NotNull(writtenContent);
        using var doc = JsonDocument.Parse(writtenContent);
        Assert.True(doc.RootElement.TryGetProperty("PluginSettings", out var pluginSettings));
        Assert.Empty(pluginSettings.EnumerateObject());

        // the freshly created (default) layer is active immediately
        Assert.Equal(string.Empty, loader.LoadSettings<TestPluginSettings>().Name);
    }

    [Fact]
    public void LoadGlobalSettings_FileNotFound_CreatesDirectoryAndWritesDefaultFile()
    {
        var file = FileMockThrowingNotFound(GlobalSettingsPath);

        var loader = new SettingsLoader(file.Object);
        loader.LoadGlobalSettings(GlobalSettingsPath);

        file.Verify(f => f.CreateDirectory(@"C:\settings\global"), Times.Once());
        file.Verify(f => f.WriteAllText(GlobalSettingsPath, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void LoadProjectSettings_FileNotFound_CreateIfNotExistFalse_WritesNothing()
    {
        var file = FileMockThrowingNotFound(ProjectSettingsPath);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath, createIfNotExist: false);

        file.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Never());
        file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        Assert.Equal(string.Empty, loader.LoadSettings<TestPluginSettings>().Name);
    }

    [Fact]
    public void LoadProjectSettings_IoException_TreatedAsMissingFileAndDefaultIsCreated()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Throws(new DirectoryNotFoundException("directory missing"));

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        file.Verify(f => f.CreateDirectory(@"C:\settings\project"), Times.Once());
        file.Verify(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void LoadProjectSettings_SecurityException_TreatedAsMissingFileAndDefaultIsCreated()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Throws(new SecurityException("access denied"));

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);

        file.Verify(f => f.CreateDirectory(@"C:\settings\project"), Times.Once());
        file.Verify(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void LoadProjectSettings_MalformedJson_ThrowsJsonException()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns("this is not valid json");

        var loader = new SettingsLoader(file.Object);

        Assert.Throws<JsonException>(() => loader.LoadProjectSettings(ProjectSettingsPath));
    }

    // ------------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------------

    [Fact]
    public void SaveProjectSettings_WritesUpdatedSettingsToFile()
    {
        // layer is loaded from an existing file so its in-memory state is non-empty
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Returns(BuildSettingsFileJson());
        string? writtenContent = null;
        file.Setup(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, contents) => writtenContent = contents);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);
        loader.SaveProjectSettings(new TestPluginSettings { Name = "saved", Count = 7 });

        file.Verify(f => f.CreateDirectory(@"C:\settings\project"), Times.Once());
        file.Verify(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()), Times.Once());

        Assert.NotNull(writtenContent);
        var persisted = ReadPersistedSettings<TestPluginSettings>(writtenContent);
        Assert.Equal("saved", persisted.Name);
        Assert.Equal(7, persisted.Count);
    }

    [Fact]
    public void SaveGlobalSettings_WritesUpdatedSettingsToFile()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(GlobalSettingsPath)).Returns(BuildSettingsFileJson());
        string? writtenContent = null;
        file.Setup(f => f.WriteAllText(GlobalSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, contents) => writtenContent = contents);

        var loader = new SettingsLoader(file.Object);
        loader.LoadGlobalSettings(GlobalSettingsPath);
        loader.SaveGlobalSettings(new TestPluginSettings { Name = "global-saved" });

        file.Verify(f => f.CreateDirectory(@"C:\settings\global"), Times.Once());
        file.Verify(f => f.WriteAllText(GlobalSettingsPath, It.IsAny<string>()), Times.Once());

        Assert.NotNull(writtenContent);
        Assert.Equal("global-saved", ReadPersistedSettings<TestPluginSettings>(writtenContent).Name);
    }

    [Fact]
    public void SaveProjectSettings_PreservesEntriesAlreadyPresentInFile()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath))
            .Returns(BuildSettingsFileJson(new OtherPluginSettings { Tag = "existing" }));
        string? writtenContent = null;
        file.Setup(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, contents) => writtenContent = contents);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);
        loader.SaveProjectSettings(new TestPluginSettings { Name = "new-entry" });

        Assert.NotNull(writtenContent);
        Assert.Equal("existing", ReadPersistedSettings<OtherPluginSettings>(writtenContent).Tag);
        Assert.Equal("new-entry", ReadPersistedSettings<TestPluginSettings>(writtenContent).Name);
    }

    [Fact]
    public void SaveProjectSettings_SavedValueIsReadableWithoutReloadingFile()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(BuildSettingsFileJson());

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);
        loader.SaveProjectSettings(new TestPluginSettings { Name = "in-memory", Count = 5 });

        var settings = loader.LoadSettings<TestPluginSettings>();

        Assert.Equal("in-memory", settings.Name);
        Assert.Equal(5, settings.Count);
    }

    [Fact]
    public void SaveProjectSettings_WrittenFileCanBeReadBackByANewLoader()
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(BuildSettingsFileJson());
        string? writtenContent = null;
        file.Setup(f => f.WriteAllText(ProjectSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, contents) => writtenContent = contents);

        var loader = new SettingsLoader(file.Object);
        loader.LoadProjectSettings(ProjectSettingsPath);
        loader.SaveProjectSettings(new TestPluginSettings { Name = "round-trip", Count = 42 });

        Assert.NotNull(writtenContent);

        var reloadFile = new Mock<IFileAccess>();
        reloadFile.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(writtenContent);
        var reloadedLoader = new SettingsLoader(reloadFile.Object);
        reloadedLoader.LoadProjectSettings(ProjectSettingsPath);

        var settings = reloadedLoader.LoadSettings<TestPluginSettings>();
        Assert.Equal("round-trip", settings.Name);
        Assert.Equal(42, settings.Count);
    }

    // ------------------------------------------------------------------
    // AllSettings
    // ------------------------------------------------------------------

    [Fact]
    public void AllSettings_GroupSettingsIsEmptyBeforeAnyLayerIsLoaded()
    {
        var loader = new SettingsLoader(new Mock<IFileAccess>().Object);

        Assert.NotNull(loader.AllSettings);
        Assert.Empty(loader.AllSettings.GroupSettings);
    }

    [Fact]
    public void AllSettings_OnlyGlobal()
    {
        var globalSettings = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 2"]
        };

        TestGroupSettingsOrder([globalSettings], [],
        [
            (SettingsKey<SerialisableSettings>(), [
                ("Setting 1", null),
                ("Setting 2", null),
            ])
        ]);
    }

    [Fact]
    public void AllSettings_OnlyProject()
    {
        var projectSettings = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 2"]
        };

        TestGroupSettingsOrder([], [projectSettings],
        [
            (SettingsKey<SerialisableSettings>(), [
                (null, "Setting 1"),
                (null, "Setting 2"),
            ])
        ]);
    }

    [Fact]
    public void AllSettings_BothSame()
    {
        var bothSettings = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 2"]
        };

        TestGroupSettingsOrder([bothSettings], [bothSettings],
        [
            (SettingsKey<SerialisableSettings>(), [
                ("Setting 1", "Setting 1"),
                ("Setting 2", "Setting 2"),
            ])
        ]);
    }

    [Fact]
    public void AllSettings_AdditionalGlobalSettings()
    {
        var onlyGlobal = new AnotherSerialisableSettings()
        {
            EntryIds = ["Setting 1"]
        };
        var bothSettings = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 2"]
        };

        TestGroupSettingsOrder([bothSettings, onlyGlobal], [bothSettings],
        [
            (SettingsKey<AnotherSerialisableSettings>(), [
                ("Setting 1", null)
            ]),
            (SettingsKey<SerialisableSettings>(), [
                ("Setting 1", "Setting 1"),
                ("Setting 2", "Setting 2"),
            ]),
        ]);
    }

    [Fact]
    public void AllSettings_AdditionalProjectSettings()
    {
        var onlyProject = new AnotherSerialisableSettings()
        {
            EntryIds = ["Setting 1"]
        };
        var bothSettings = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 2"]
        };

        TestGroupSettingsOrder([bothSettings], [bothSettings, onlyProject],
        [
            (SettingsKey<AnotherSerialisableSettings>(), [
                (null, "Setting 1")
            ]),
            (SettingsKey<SerialisableSettings>(), [
                ("Setting 1", "Setting 1"),
                ("Setting 2", "Setting 2"),
            ]),
        ]);
    }

    [Fact]
    public void AllSettings_MissingSettingsInSameType()
    {
        var onlyGlobal = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting Global", "Setting 3", "Setting 5"]
        };
        var onlyProject = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 3", "Setting 4", "Setting 5"]
        };

        TestGroupSettingsOrder([onlyGlobal], [onlyProject],
        [
            (SettingsKey<SerialisableSettings>(), [
                ("Setting 1", "Setting 1"),
                ("Setting Global", null),
                ("Setting 3", "Setting 3"),
                (null, "Setting 4"),
                ("Setting 5", "Setting 5"),
            ]),
        ]);
    }

    [Fact]
    public void AllSettings_DifferentOrders_GlobalIsPreferred()
    {
        var onlyGlobal = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 2", "Setting 3", "Setting 4"]
        };
        var onlyProject = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 3", "Setting 2", "Setting 4"]
        };

        TestGroupSettingsOrder([onlyGlobal], [onlyProject],
        [
            (SettingsKey<SerialisableSettings>(), [
                ("Setting 1", "Setting 1"),
                ("Setting 2", "Setting 2"),
                ("Setting 3", "Setting 3"),
                ("Setting 4", "Setting 4"),
            ]),
        ]);
    }

    [Fact]
    public void AllSettings_DifferentOrdersSomeMissing_GlobalIsPreferred()
    {
        var onlyGlobal = new SerialisableSettings()
        {
            EntryIds = ["Setting 2", "Setting 3", "Setting 4"]
        };
        var onlyProject = new SerialisableSettings()
        {
            EntryIds = ["Setting 1", "Setting 3", "Setting 2", "Setting 4"]
        };

        TestGroupSettingsOrder([onlyGlobal], [onlyProject],
        [
            (SettingsKey<SerialisableSettings>(), [
                (null, "Setting 1"),
                ("Setting 2", "Setting 2"),
                ("Setting 3", "Setting 3"),
                ("Setting 4", "Setting 4"),
            ]),
        ]);
    }

    /// <param name="expectedGroupSettings">A list of pairs of the GroupSetting.Name and a list of its entries,
    /// represented by a pair of its Global.Id as well as Project.Id</param>
    public void TestGroupSettingsOrder(ISettings[] globalSettings, ISettings[] projectSettings,
        List<(string, List<(string?, string?)>)> expectedGroupSettings)
    {
        var file = new Mock<IFileAccess>();
        file.Setup(f => f.ReadAllText(GlobalSettingsPath)).Returns(BuildSettingsFileJson(globalSettings));
        file.Setup(f => f.ReadAllText(ProjectSettingsPath)).Returns(BuildSettingsFileJson(projectSettings));
        var loader = new SettingsLoader(file.Object);

        loader.LoadGlobalSettings(GlobalSettingsPath);
        loader.LoadProjectSettings(ProjectSettingsPath);

        Assert.NotNull(loader.AllSettings);
        var allSettingsGroupSettings = loader.AllSettings.GroupSettings;
        Assert.Equal(
            expectedGroupSettings.Select(a => a.Item1),
            allSettingsGroupSettings.Select(gs => gs.Name));

        Assert.Equal(expectedGroupSettings.Count, allSettingsGroupSettings.Count);
        for (var i = 0; i < expectedGroupSettings.Count; i++)
        {
            Assert.Equal(
                expectedGroupSettings[i].Item2,
                allSettingsGroupSettings[i].Entries.Select(entry => (entry.Global?.Id, entry.Project?.Id))
            );
        }
    }

    // ------------------------------------------------------------------
    // Helpers & test doubles
    // ------------------------------------------------------------------

    private static T ReadPersistedSettings<T>(string json)
        where T : class
    {
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement.GetProperty("PluginSettings").GetProperty(SettingsKey<T>());
        return element.Deserialize<T>()!;
    }
}

public sealed class TestPluginSettings : ISettings
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }

    public List<ISettings.SettingsEntry> GetEntries() => [];
}

public sealed class OtherPluginSettings : ISettings
{
    public string Tag { get; set; } = string.Empty;

    public List<ISettings.SettingsEntry> GetEntries() => [];
}

public class SerialisableSettings : ISettings
{
    public List<string> EntryIds { get; set; } = [];

    private List<ISettings.SettingsEntry>? entries = null;

    public List<ISettings.SettingsEntry> GetEntries()
    {
        return entries ??= GenerateSettingsEntries(EntryIds);
    }

    private static List<ISettings.SettingsEntry> GenerateSettingsEntries(List<string> entries)
    {
        return entries
            .Select(id => new ISettings.SettingsEntry(
                true, id, ISettings.SettingsType.String, "Name:" + id, null,
                () => throw new NotImplementedException(), value => throw new NotImplementedException()))
            .ToList();
    }
}

public sealed class AnotherSerialisableSettings : SerialisableSettings;