using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ablinger.MyAiHarness.Core.Utils.Serialisation;

public class ExistingReferenceHandler(IEnumerable<ISerialisationIdentifiable> existingElements) : ReferenceHandler
{
    private readonly ExistingReferenceResolver referenceResolver = new(existingElements);
    
    public override ReferenceResolver CreateResolver()
    {
        return referenceResolver;
    }
}

public sealed class ExistingReferenceResolver(IEnumerable<ISerialisationIdentifiable> existingElements) : ReferenceResolver
{
    public override void AddReference(string referenceId, object value)
    {
        throw new System.NotImplementedException();
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        throw new System.NotImplementedException();
    }

    public override object ResolveReference(string referenceId)
    {
        throw new System.NotImplementedException();
    }
}