using System;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Utils.Serialisation;

namespace Ablinger.MyAiHarness.Core.Harness.Conversations;

[JsonDerivedType(typeof(MessageConversationPoint), typeDiscriminator: "message")]
[JsonDerivedType(typeof(ThinkingConversationPoint), typeDiscriminator: "thinking")]
public abstract class ConversationPoint
{
    private ConversationPoint()
    {
    }
    
    [JsonConverter(typeof(ExistingElementLinker<Conversation>))]
    public required Conversation Conversation { get; init; }

    [JsonConverter(typeof(ExistingElementLinker<ConversationMember>))]
    public required ConversationMember Author { get; init; }

    /// <summary>
    /// Additional information about where it came from.
    /// For a LLM, this could for instance be "automated job", or the LLM's model.
    /// For a user, it could signify where it was sent from (chat window, filling out a popup etc.).
    /// 
    /// <para>Generally, this should not be included in the prompt for LLMs.</para>
    /// </summary>
    public string? Source { get; init; }
   
    /// <summary>
    /// In case of streams, this is the timestamp the answer started to be streamed.
    /// </summary>
    public required DateTime Timestamp { get; init; }
    
    // ==================== Implementations ====================
    public sealed class MessageConversationPoint : ConversationPoint
    {
        public required MessageContent Content { get; init; }
    }

    public sealed class ThinkingConversationPoint : ConversationPoint
    {
        public required string Content { get; init; }
    }
}
