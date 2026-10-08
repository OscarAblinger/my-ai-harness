using System.Text.Json.Serialization;

namespace Ablinger.MyAiHarness.Core.Utils.Serialisation;

public interface ISerialisationIdentifiable
{
    [JsonIgnore]
    public string SerialisationId { get; }
}