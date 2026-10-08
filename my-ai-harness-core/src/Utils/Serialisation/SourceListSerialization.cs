namespace Ablinger.MyAiHarness.Core.Utils.Serialisation;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicData;

public class SourceListJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        // Check if the type is SourceList<T>
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(SourceList<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(SourceListConverter<>).MakeGenericType(elementType);
        return Activator.CreateInstance(converterType) as JsonConverter;
    }
}

public class SourceListConverter<T> : JsonConverter<SourceList<T>>
    where T : notnull
{
    public override SourceList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);

        var sourceList = new SourceList<T>();
        if (list != null)
        {
            sourceList.AddRange(list);
        }

        return sourceList;
    }

    public override void Write(Utf8JsonWriter writer, SourceList<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Items, options);
    }
}