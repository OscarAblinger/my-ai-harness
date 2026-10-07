using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Projects;
using Ablinger.MyAiHarness.Core.Utils;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Conversations;

public struct Conversation
{
    public Project Project { get; init; }
    
    public string Name { get; init; }
    
    [JsonConverter(typeof(SourceListJsonConverterFactory))]
    public SourceList<ConversationPoint> ConversationPoints { get; init; }
}
