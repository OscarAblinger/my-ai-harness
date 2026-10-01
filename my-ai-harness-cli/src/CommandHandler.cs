using System.CommandLine;
using System.CommandLine.Invocation;
using Ablinger.MyAiHarness.Core.Plugins;

namespace Ablinger.MyAiHarness.CLI;

public class CommandHandler
{
    private readonly SettingsLoader settingsLoader;
    private readonly RootCommand rootCommand = new();
    
    private readonly Argument<SettingsType> settingType = new("global or project")
    {
        Description = "Whether to access the global or project level settings.",
        Arity = ArgumentArity.ExactlyOne,
    };
    private readonly Argument<string> optionalSettingId = new("setting ID")
    {
        Description = "The ID of the setting. May refer to a partial one. E.g. 'Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors.OpenAILikeLLMSourceSettings' or 'Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors.OpenAILikeLLMSourceSettings.Endpoint'",
        Arity = ArgumentArity.ZeroOrOne,
    };

    public CommandHandler(SettingsLoader settingsLoader)
    {
        this.settingsLoader = settingsLoader;
        rootCommand.Subcommands.Add(new Command("settings", "Accesses settings.")
        {
            Subcommands =
            {
                new Command("list", "Lists all settings.")
                {
                    Arguments =
                    {
                        settingType,
                        optionalSettingId
                    },
                    Action = new SettingsListAction(this)
                },
                new Command("edit", "Edits settings.")
                {
                    Arguments =
                    {
                        settingType,
                        optionalSettingId
                    }
                },
                new Command("pwd", "Prints all locations used to find settings."),
            }
        });
    }
    
    public void Handle(string command)
    {
        rootCommand.Parse(command).Invoke();
    }

    enum SettingsType
    {
        Global,
        Project
    }

    private sealed class SettingsListAction(CommandHandler commandHandler) : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            throw new System.NotImplementedException();
        }
    }
}