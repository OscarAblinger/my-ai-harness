using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ablinger.MyAiHarness.Core.Harness.Projects;
using Ablinger.MyAiHarness.Core.Utils.Serialisation;
using DynamicData;

namespace Ablinger.MyAiHarness.Core.Harness.Conversations;

[JsonConverter(typeof(ConversationConverterFactory))]
public readonly record struct Conversation : ISerialisationIdentifiable
{
    public required Project Project { get; init; }

    public required string Name { get; init; }

    public string SerialisationId => Name;

    public required SourceList<ConversationPoint> ConversationPoints { get; init; }
}

/// <summary>
/// Manual JSON serialiser, because System.Text.Json can not resolve references across custom converters (which would be
/// needed for a SourceList anyways).
/// </summary>
public class ConversationConverterFactory : JsonConverter<Conversation>
{
    public override Conversation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.Read())
        {
            Expect(reader, JsonTokenType.StartObject);
        }

        Project? project = null;
        string? name = null;
        List<ConversationPoint>? conversationPoints = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            Expect(reader, JsonTokenType.PropertyName);

            switch (reader.GetString())
            {
                case nameof(Conversation.Name):
                    Next(reader);
                    name = reader.GetString();
                    break;
                case nameof(Conversation.Project):
                    Next(reader);
                    project = JsonSerializer.Deserialize<Project>(ref reader, options);
                    break;
                case nameof(Conversation.ConversationPoints):
                    Next(reader);
                    conversationPoints = JsonSerializer.Deserialize<List<ConversationPoint>>(ref reader, options);
                    break;
                default:
                    throw new JsonException(
                        $"Unsupported property found in ${nameof(Conversation)}: ${reader.GetString()}");
            }
        }

        if (project == null || name == null || conversationPoints == null)
        {
            throw new JsonException($"""
                                      Missing required json entries for ${nameof(Conversation)}:
                                        ${nameof(Conversation.Name)} = ${name}
                                        ${nameof(Conversation.Project)} = ${project}
                                        ${nameof(Conversation.ConversationPoints)} = ${conversationPoints}
                                      """);
        }

        var sourceList = new SourceList<ConversationPoint>();
        sourceList.AddRange(conversationPoints);
        return new Conversation()
        {
            Project = project,
            Name = name,
            ConversationPoints = sourceList
        };
    }

    private static void Next(Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException($"Malformed JSON. Expected next element, but end of input found.");
        }
    }

    private static void Expect(Utf8JsonReader reader, JsonTokenType expectedTokenType)
    {
        if (reader.TokenType != expectedTokenType)
        {
            throw new JsonException($"Malformed JSON. Expected ${expectedTokenType}, but found ${reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, Conversation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var propertyInfo in typeof(Conversation).GetProperties())
        {
            switch (propertyInfo.Name)
            {
                case nameof(Conversation.Project):
                    writer.WriteString(nameof(Conversation.Project), value.Project.Name);
                    break;
                case nameof(Conversation.ConversationPoints):
                    JsonSerializerOptions withParent = new(options);
                    withParent.Converters.Add(new ExistingElementStorageConverter([value]));
                    JsonSerializer.Serialize(writer, value.ConversationPoints.Items, options);
                    break;
                default:
                    JsonSerializer.Serialize(writer, propertyInfo.GetValue(value), options);
                    break;
            }
        }

        writer.WriteEndObject();
    }
}