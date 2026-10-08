using Ablinger.MyAiHarness.Core.Utils.Serialisation;

namespace Ablinger.MyAiHarness.Core.Harness.Conversations;

public class ConversationMember : ISerialisationIdentifiable
{
    public required string Name { get; init; }
    
    public string SerialisationId => Name;
    
    public required bool IsHuman { get; init; }
}