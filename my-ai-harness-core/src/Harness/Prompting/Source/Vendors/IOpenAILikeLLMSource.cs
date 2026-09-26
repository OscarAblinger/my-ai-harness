using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Ablinger.MyAiHarness.Core.Harness.Prompting.Source.Vendors;

public interface IOpenAILikeLLMSource : ILLMSource
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public Task<ImmediateCompletionResponse> SendCompletion(CompletionRequest request,
        CancellationToken cancellationToken);

    public record struct CompletionRequest
    {
        public string Model { get; set; }
        public string Prompt { get; set; }

        /// <summary>
        /// Number between -2.0 and 2.0.
        /// Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the
        /// model's likelihood to repeat the same line verbatim.
        /// 
        /// <see href="https://developers.openai.com/api/docs/guides/text">See more information about frequency and
        /// presence penalties.</see>
        /// </summary>
        public int? FrequencyPenalty { get; set; }

        /// <summary>
        /// The maximum number of tokens that can be generated in the completion.
        ///
        /// The token count of your prompt plus max_tokens cannot exceed the model's context length.
        /// Example Python code for counting tokens.
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// How many completions to generate for each prompt.
        /// 
        /// Note: Because this parameter generates many completions, it can quickly consume your token quota.
        /// Use carefully and ensure that you have reasonable settings for max_tokens and stop.
        /// </summary>
        public int? NumberOfCompletions { get; set; }

        /// <summary>
        /// Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so
        /// far, increasing the model's likelihood to talk about new topics.
        ///
        /// <see href="https://developers.openai.com/api/docs/guides/text">See more information about frequency and
        /// presence penalties.</see>
        /// </summary>
        public int? PresencePenalty { get; set; }

        /// <summary>
        /// What sampling temperature to use, between 0 and 2.
        /// Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more
        /// focused and deterministic.
        ///
        /// We generally recommend altering this or top_p but not both.
        /// </summary>
        public int? Temperature { get; set; }

        /// <summary>
        /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results
        /// of the tokens with top_p probability mass.
        /// So 0.1 means only the tokens comprising the top 10% probability mass are considered.
        ///
        /// We generally recommend altering this or temperature but not both.
        /// </summary>
        public int? TopP { get; set; }
    }

    public record struct ImmediateCompletionResponse
    {
        public string Id { get; set; }
        public Choice[] Choices { get; set; }
        public int Created { get; set; }
        public string Model { get; set; }

        public string GetBestResponse()
        {
            return Choices[0].Text;
        }
    }

    public record struct Choice
    {
        public string Text { get; set; }
        public int Index { get; set; }
        public FinishReason FinishReason { get; set; }
    }

    public enum FinishReason
    {
        Stop,
        Length,
        ContentFilter
    }
}