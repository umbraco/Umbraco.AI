using Umbraco.Ai.Core.Tests;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AiTestTranscript"/> domain models and <see cref="AiTestTranscriptEntity"/> database entities.
/// </summary>
internal static class AiTestTranscriptFactory
{
    /// <summary>
    /// Creates an <see cref="AiTestTranscript"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiTestTranscript BuildDomain(AiTestTranscriptEntity entity)
    {
        return new AiTestTranscript
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
    /// Creates an <see cref="AiTestTranscriptEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="transcript">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiTestTranscriptEntity BuildEntity(AiTestTranscript transcript)
    {
        return new AiTestTranscriptEntity
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
}
