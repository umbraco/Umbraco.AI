using System.Text.Json;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AITestTranscript"/> domain models and <see cref="AITestTranscriptEntity"/> database entities.
/// </summary>
internal static class AITestTranscriptFactory
{
    /// <summary>
    /// Creates an <see cref="AITestTranscript"/> domain model from a database entity.
    /// </summary>
    public static AITestTranscript BuildDomain(AITestTranscriptEntity entity)
    {
        return new AITestTranscript
        {
            Id = entity.Id,
            RunId = entity.RunId,
            Messages = ParseJson(entity.MessagesJson),
            ToolCalls = ParseJson(entity.ToolCallsJson),
            Reasoning = ParseJson(entity.ReasoningJson),
            Timing = ParseJson(entity.TimingJson),
            FinalOutput = ParseJson(entity.FinalOutputJson) ?? default
        };
    }

    /// <summary>
    /// Creates an <see cref="AITestTranscriptEntity"/> database entity from a domain model.
    /// </summary>
    public static AITestTranscriptEntity BuildEntity(AITestTranscript transcript)
    {
        return new AITestTranscriptEntity
        {
            Id = transcript.Id,
            RunId = transcript.RunId,
            MessagesJson = ToJsonString(transcript.Messages),
            ToolCallsJson = ToJsonString(transcript.ToolCalls),
            ReasoningJson = ToJsonString(transcript.Reasoning),
            TimingJson = ToJsonString(transcript.Timing),
            FinalOutputJson = transcript.FinalOutput.GetRawText()
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AITestTranscriptEntity"/> with values from a domain model.
    /// </summary>
    public static void UpdateEntity(AITestTranscriptEntity entity, AITestTranscript transcript)
    {
        entity.RunId = transcript.RunId;
        entity.MessagesJson = ToJsonString(transcript.Messages);
        entity.ToolCallsJson = ToJsonString(transcript.ToolCalls);
        entity.ReasoningJson = ToJsonString(transcript.Reasoning);
        entity.TimingJson = ToJsonString(transcript.Timing);
        entity.FinalOutputJson = transcript.FinalOutput.GetRawText();
    }

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(json, Constants.DefaultJsonSerializerOptions);
    }

    private static string? ToJsonString(JsonElement? element)
        => element?.GetRawText();
}
