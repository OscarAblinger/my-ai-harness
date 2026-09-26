using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Ablinger.MyAiHarness.Core.Plugins;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;

namespace Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;

public class OpenAILikeLLMSource(SettingsLoader settingsLoader) : IOpenAILikeLLMSource
{
    private readonly OpenAILikeLLMSourceSettings settings = settingsLoader.LoadSettings<OpenAILikeLLMSourceSettings>();
    
    public string SendSimplePrompt(string prompt)
    {
        if (settings.DefaultModel == null)
        {
            return "No default model specified!";
        }

        return SendCompletion(new IOpenAILikeLLMSource.CompletionRequest()
        {
            Model = settings.DefaultModel,
            Prompt = prompt
        }, CancellationToken.None).Result.GetBestResponse();
    }

    public async Task<IOpenAILikeLLMSource.ImmediateCompletionResponse> SendCompletion(
        IOpenAILikeLLMSource.CompletionRequest request, CancellationToken cancellationToken)
    {
        if (settings.Endpoint == null)
        {
            throw new ArgumentException("No Endpoint set.");
        }
        
        var completionsEndpoint = new Uri(settings.Endpoint.TrimEnd('/') + "/completions");
        var requestBody = JsonSerializer.Serialize(request, IOpenAILikeLLMSource.JsonSerializerOptions);

        using HttpClient client = new();
        if (settings.APIKey != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.APIKey);
        }
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var response = await client.PostAsync(completionsEndpoint, new StringContent(requestBody, Encoding.UTF8, "application/json"), cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // todo: proper error handling
            throw new Exception("Failed to get a successful response. Response gotten: " + response);
        }

        return JsonSerializer.Deserialize<IOpenAILikeLLMSource.ImmediateCompletionResponse>(responseBody, IOpenAILikeLLMSource.JsonSerializerOptions);
    }
}

public class OpenAILikeLLMSourceSettings : ISettings.SimpleAttributeBasedSettings
{
    [Entry(description: "Base URL of the LLM Provider. E.g. https://openrouter.ai/api/v1")]
    public string? Endpoint { get; set; }

    [Entry(description: "API Key to authenticate.")]
    public string? APIKey { get; set; }
    
    [Entry(description: "The default LLM model to use.")]
    public string? DefaultModel { get; set; }
}