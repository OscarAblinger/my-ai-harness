using System;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Conversations;
using Ablinger.MyAiHarness.Core.Utils.Serialisation;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Projects;

public class Project : ISerialisationIdentifiable
{
    public required string Name { get; init; }

    public string SerialisationId => Name;

    public required string Path { get; init; }

    public required bool Global { get; init; }

    [JsonIgnore]
    public IObservableList<IObservable<Conversation>> Conversations { get; init; } =
        new SourceList<IObservable<Conversation>>();
}