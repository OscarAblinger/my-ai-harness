using Ablinger.MyAiHarness.Core.Harness.Conversations;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Projects;

public class Project
{
    public required string Name { get; init; }
    
    public required string Path { get; init; }
 
    public required bool Global { get; init; }
    
    public SourceList<Conversation> Conversations { get; init; } = new();
}