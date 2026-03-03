using System.Text.Json;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AITestRun"/> domain models and <see cref="AITestRunEntity"/> database entities.
/// </summary>
internal static class AITestRunFactory
{
    /// <summary>
    /// Creates an <see cref="AITestRun"/> domain model from a database entity.
    /// </summary>
    public static AITestRun BuildDomain(AITestRunEntity entity)
    {
        IReadOnlyList<Guid> contextIds = Array.Empty<Guid>();
        if (!string.IsNullOrEmpty(entity.ContextIds))
        {
            contextIds = entity.ContextIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Guid.Parse)
                .ToList();
        }

        IReadOnlyList<AITestGraderResult> graderResults = Array.Empty<AITestGraderResult>();
        if (!string.IsNullOrEmpty(entity.GraderResultsJson))
        {
            graderResults = (IReadOnlyList<AITestGraderResult>?)JsonSerializer.Deserialize<List<AITestGraderResult>>(entity.GraderResultsJson, Constants.DefaultJsonSerializerOptions)
                ?? Array.Empty<AITestGraderResult>();
        }

        AITestTokenUsage? tokenUsage = null;
        if (!string.IsNullOrEmpty(entity.OutcomeTokenUsageJson))
        {
            tokenUsage = JsonSerializer.Deserialize<AITestTokenUsage>(entity.OutcomeTokenUsageJson, Constants.DefaultJsonSerializerOptions);
        }

        AITestRunError? error = null;
        if (!string.IsNullOrEmpty(entity.MetadataJson))
        {
            error = JsonSerializer.Deserialize<AITestRunError>(entity.MetadataJson, Constants.DefaultJsonSerializerOptions);
        }

        return new AITestRun
        {
            Id = entity.Id,
            TestId = entity.TestId,
            TestVersion = entity.TestVersion,
            RunNumber = entity.RunNumber,
            ProfileId = entity.ProfileId,
            ContextIds = contextIds,
            ExecutedAt = entity.ExecutedAt,
            ExecutedByUserId = entity.ExecutedByUserId,
            Status = (AITestRunStatus)entity.Status,
            DurationMs = entity.DurationMs,
            TranscriptId = entity.TranscriptId,
            Outcome = new AITestOutcome
            {
                OutputType = (AITestOutputType)entity.OutcomeType,
                OutputValue = entity.OutcomeValue,
                FinishReason = entity.OutcomeFinishReason,
                TokenUsage = tokenUsage
            },
            GraderResults = graderResults,
            Error = error,
            BatchId = entity.BatchId,
            ExecutionId = entity.ExecutionId,
            VariationId = entity.VariationId,
            VariationName = entity.VariationName
        };
    }

    /// <summary>
    /// Creates an <see cref="AITestRunEntity"/> database entity from a domain model.
    /// </summary>
    public static AITestRunEntity BuildEntity(AITestRun run)
    {
        return new AITestRunEntity
        {
            Id = run.Id,
            TestId = run.TestId,
            TestVersion = run.TestVersion,
            RunNumber = run.RunNumber,
            ProfileId = run.ProfileId,
            ContextIds = run.ContextIds.Count > 0 ? string.Join(',', run.ContextIds) : null,
            ExecutedAt = run.ExecutedAt,
            ExecutedByUserId = run.ExecutedByUserId,
            Status = (int)run.Status,
            DurationMs = run.DurationMs,
            TranscriptId = run.TranscriptId,
            OutcomeType = run.Outcome != null ? (int)run.Outcome.OutputType : 0,
            OutcomeValue = run.Outcome?.OutputValue,
            OutcomeFinishReason = run.Outcome?.FinishReason,
            OutcomeTokenUsageJson = run.Outcome?.TokenUsage != null ? JsonSerializer.Serialize(run.Outcome.TokenUsage, Constants.DefaultJsonSerializerOptions) : null,
            GraderResultsJson = run.GraderResults.Count > 0 ? JsonSerializer.Serialize(run.GraderResults, Constants.DefaultJsonSerializerOptions) : null,
            MetadataJson = run.Error != null ? JsonSerializer.Serialize(run.Error, Constants.DefaultJsonSerializerOptions) : null,
            BatchId = run.BatchId,
            ExecutionId = run.ExecutionId,
            VariationId = run.VariationId,
            VariationName = run.VariationName
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AITestRunEntity"/> with values from a domain model.
    /// </summary>
    public static void UpdateEntity(AITestRunEntity entity, AITestRun run)
    {
        entity.TestVersion = run.TestVersion;
        entity.RunNumber = run.RunNumber;
        entity.ProfileId = run.ProfileId;
        entity.ContextIds = run.ContextIds.Count > 0 ? string.Join(',', run.ContextIds) : null;
        entity.ExecutedAt = run.ExecutedAt;
        entity.ExecutedByUserId = run.ExecutedByUserId;
        entity.Status = (int)run.Status;
        entity.DurationMs = run.DurationMs;
        entity.TranscriptId = run.TranscriptId;
        entity.OutcomeType = run.Outcome != null ? (int)run.Outcome.OutputType : 0;
        entity.OutcomeValue = run.Outcome?.OutputValue;
        entity.OutcomeFinishReason = run.Outcome?.FinishReason;
        entity.OutcomeTokenUsageJson = run.Outcome?.TokenUsage != null ? JsonSerializer.Serialize(run.Outcome.TokenUsage, Constants.DefaultJsonSerializerOptions) : null;
        entity.GraderResultsJson = run.GraderResults.Count > 0 ? JsonSerializer.Serialize(run.GraderResults, Constants.DefaultJsonSerializerOptions) : null;
        entity.MetadataJson = run.Error != null ? JsonSerializer.Serialize(run.Error, Constants.DefaultJsonSerializerOptions) : null;
        entity.BatchId = run.BatchId;
        entity.ExecutionId = run.ExecutionId;
        entity.VariationId = run.VariationId;
        entity.VariationName = run.VariationName;
    }
}
