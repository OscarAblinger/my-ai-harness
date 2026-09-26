namespace Ablinger.MyAiHarness.Core.Harness.Prompting.Source;

public interface ILLMSource
{
    /// <summary>
    /// The simplest implementation that sends a prompt and returns the LLM's answer.
    /// This has to be supported by all implementations.
    /// </summary>
    string SendSimplePrompt(string prompt);
}