using Umbraco.Ai.Core.Tests;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AiTestRun"/> domain models and <see cref="AiTestRunEntity"/> database entities.
/// </summary>
internal static class AiTestRunFactory
{
    /// <summary>
    /// Creates an <see cref="AiTestRun"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiTestRun BuildDomain(AiTestRunEntity entity)
    {
        return new AiTestRun
        {
            Id = entity.Id,
            TestId = entity.TestId,
            TestVersion = entity.TestVersion,
            RunNumber = entity.RunNumber,
            ProfileId = entity.ProfileId,
            ContextIdsJson = entity.ContextIdsJson,
            ExecutedAt = entity.ExecutedAt,
            ExecutedByUserId = entity.ExecutedByUserId,
            Status = (AiTestRunStatus)entity.Status,
            DurationMs = entity.DurationMs,
            ErrorMessage = entity.ErrorMessage,
            Outcome = new AiTestOutcome
            {
                OutputType = (AiTestOutcomeType)entity.OutcomeType,
                OutputValue = entity.OutcomeValue,
                FinishReason = entity.FinishReason,
                InputTokens = entity.InputTokens,
                OutputTokens = entity.OutputTokens
            },
            Transcript = entity.Transcript != null ? AiTestTranscriptFactory.BuildDomain(entity.Transcript) : null,
            GraderResults = entity.GraderResults.Select(AiTestGraderResultFactory.BuildDomain).ToList(),
            MetadataJson = entity.MetadataJson,
            BatchId = entity.BatchId
        };
    }

    /// <summary>
    /// Creates an <see cref="AiTestRunEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="run">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiTestRunEntity BuildEntity(AiTestRun run)
    {
        var entity = new AiTestRunEntity
        {
            Id = run.Id,
            TestId = run.TestId,
            TestVersion = run.TestVersion,
            RunNumber = run.RunNumber,
            ProfileId = run.ProfileId,
            ContextIdsJson = run.ContextIdsJson,
            ExecutedAt = run.ExecutedAt,
            ExecutedByUserId = run.ExecutedByUserId,
            Status = (int)run.Status,
            DurationMs = run.DurationMs,
            ErrorMessage = run.ErrorMessage,
            MetadataJson = run.MetadataJson,
            BatchId = run.BatchId
        };

        if (run.Outcome != null)
        {
            entity.OutcomeType = (int)run.Outcome.OutputType;
            entity.OutcomeValue = run.Outcome.OutputValue;
            entity.FinishReason = run.Outcome.FinishReason;
            entity.InputTokens = run.Outcome.InputTokens;
            entity.OutputTokens = run.Outcome.OutputTokens;
        }

        if (run.Transcript != null)
        {
            entity.Transcript = AiTestTranscriptFactory.BuildEntity(run.Transcript);
        }

        entity.GraderResults = run.GraderResults.Select(AiTestGraderResultFactory.BuildEntity).ToList();

        return entity;
    }
}
