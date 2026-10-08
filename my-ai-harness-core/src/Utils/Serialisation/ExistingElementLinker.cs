using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ablinger.MyAiHarness.Core.Utils.Serialisation;

public class ExistingElementLinker<T> : JsonConverterFactory
    where T : ISerialisationIdentifiable
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsAssignableTo(typeof(T));
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var existingElementJsonConverter = options.Converters
            .OfType<ExistingElementStorageConverter>()
            .First();

        return new ExistingElementConverter(existingElementJsonConverter);
    }
}

internal class ExistingElementConverter(ExistingElementStorageConverter existingElementStorageConverter) : JsonConverter<ISerialisationIdentifiable>
{
    public override ISerialisationIdentifiable? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var serializationId = reader.GetString();
        if (serializationId == null)
        {
            throw new JsonException("Could not find expected serializationId.");
        }

        return existingElementStorageConverter.GetElementForId(typeToConvert, serializationId);
    }

    public override void Write(Utf8JsonWriter writer, ISerialisationIdentifiable value, JsonSerializerOptions options)
    {
        writer.WriteString(nameof(ISerialisationIdentifiable.SerialisationId), value.SerialisationId);
    }
}

public class ExistingElementStorageConverter(IEnumerable<ISerialisationIdentifiable> existingElements)
    : JsonConverter<ExistingElement>
{
    private readonly Dictionary<(Type, string), ISerialisationIdentifiable> idsToElements =
        existingElements.ToDictionary(si => (si.GetType(), si.SerialisationId));
    
    public override ExistingElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ExistingElement value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public ISerialisationIdentifiable? GetElementForId(Type type, string serializationId)
    {
        if (idsToElements.TryGetValue((type, serializationId), out var si))
        {
            return si;
        }
        
        throw new MissingExpectedElement(type, serializationId, idsToElements);
    }
}

public sealed class MissingExpectedElement(
    Type type,
    string serializationId,
    Dictionary<(Type, string), ISerialisationIdentifiable> idsToElements) :
    ArgumentException($"No existing element found for ${serializationId} of type ${type}. Known ids: ${idsToElements}");

/// <summary>
/// Placeholder type to ensure it is never actually used as a serialiser.
/// </summary>
// ReSharper disable once ClassCannotBeInstantiated
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ExistingElement
{
    /// <summary>
    /// Don't allow construction of this class.
    /// </summary>
    private ExistingElement() {}
}