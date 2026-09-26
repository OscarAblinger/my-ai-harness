using Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;

namespace Ablinger.MyAiHarness.Core.Harness.Prompting;

public class PromptProcessor(IOpenAILikeLLMSource openAiLikeLlmSource)
{
    public string ProcessPrompt(string prompt)
    {
        return openAiLikeLlmSource.SendSimplePrompt(prompt);
    }
}