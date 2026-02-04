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
            MessagesJson = entity.MessagesJson,
            ToolCallsJson = entity.ToolCallsJson,
            ReasoningJson = entity.ReasoningJson,
            TimingJson = entity.TimingJson,
            FinalOutputJson = entity.FinalOutputJson
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
            MessagesJson = transcript.MessagesJson,
            ToolCallsJson = transcript.ToolCallsJson,
            ReasoningJson = transcript.ReasoningJson,
            TimingJson = transcript.TimingJson,
            FinalOutputJson = transcript.FinalOutputJson
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AITestTranscriptEntity"/> with values from a domain model.
    /// </summary>
    public static void UpdateEntity(AITestTranscriptEntity entity, AITestTranscript transcript)
    {
        entity.RunId = transcript.RunId;
        entity.MessagesJson = transcript.MessagesJson;
        entity.ToolCallsJson = transcript.ToolCallsJson;
        entity.ReasoningJson = transcript.ReasoningJson;
        entity.TimingJson = transcript.TimingJson;
        entity.FinalOutputJson = transcript.FinalOutputJson;
    }
}
