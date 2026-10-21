using Ablinger.MyAiHarness.Core.Utils.Serialisation;

namespace Ablinger.MyAiHarness.Core.Harness.Conversations;

public class ConversationMember
{
    public required string Name { get; init; }
    
    public required bool IsHuman { get; init; }
}