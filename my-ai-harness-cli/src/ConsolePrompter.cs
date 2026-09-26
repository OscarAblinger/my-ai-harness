using System;
using System.Threading;
using System.Threading.Tasks;
using Ablinger.MyAiHarness.Core.Harness;
using Ablinger.MyAiHarness.Core.Harness.Prompting;
using RadLine;
using Spectre.Console;

namespace Ablinger.MyAiHarness.CLI;

public class ConsolePrompter : IPrompter
{
    private readonly PromptProcessor promptProcessor;
    private LineEditor editor;
    
    public ConsolePrompter(PromptProcessor promptProcessor)
    {
        this.promptProcessor = promptProcessor;
        editor = new LineEditor()
        {
            MultiLine = true,
            // TODO: Completion = slashCompletion
        };
    }

    public Task Register(Harness harness)
    {
        return Task.Run(async () =>
        {
            Console.WriteLine("Enter your prompts.");
            Console.WriteLine("Prefix with '/' to access settings/commands.");
            Console.WriteLine("Press Shift+Enter or Ctrl+Enter to send to the LLM.");
            while (true)
            {
                await HandleNextPrompt();
            }
            // ReSharper disable once FunctionNeverReturns
        }); // TODO: cancellation token for shutdown
    }

    private async Task HandleNextPrompt()
    {
        var line = await editor.ReadLine(CancellationToken.None);

        if (line == null)
        {
            return;
        }

        if (line.TrimStart().StartsWith('/'))
        {
            // TODO
            Console.WriteLine("Slash commands not implemented yet!");
        }
        else
        {
            var answer = promptProcessor.ProcessPrompt(line);
            Console.Write(answer);
            Console.WriteLine();
        }
    }
}