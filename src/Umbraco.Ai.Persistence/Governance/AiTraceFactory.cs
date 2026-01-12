using Umbraco.Ai.Core.Governance;

namespace Umbraco.Ai.Persistence.Governance;

/// <summary>
/// Factory for mapping between <see cref="AiTrace"/> domain models and <see cref="AiTraceEntity"/> database entities.
/// </summary>
internal static class AiTraceFactory
{
    /// <summary>
    /// Creates an <see cref="AiTrace"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiTrace BuildDomain(AiTraceEntity entity)
    {
        var spans = entity.Spans
            .Select(AiExecutionSpanFactory.BuildDomain)
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        return new AiTrace
        {
            Id = entity.Id,
            TraceId = entity.TraceId,
            SpanId = entity.SpanId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Status = (AiTraceStatus)entity.Status,
            ErrorCategory = entity.ErrorCategory.HasValue ? (AiTraceErrorCategory)entity.ErrorCategory.Value : null,
            ErrorMessage = entity.ErrorMessage,
            UserId = entity.UserId,
            UserName = entity.UserName,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            OperationType = entity.OperationType,
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            FeatureType = entity.FeatureType,
            FeatureId = entity.FeatureId,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            PromptSnapshot = entity.PromptSnapshot,
            ResponseSnapshot = entity.ResponseSnapshot,
            DetailLevel = (AiTraceDetailLevel)entity.DetailLevel,
            Spans = spans
        };
    }

    /// <summary>
    /// Creates an <see cref="AiTraceEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="trace">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiTraceEntity BuildEntity(AiTrace trace)
    {
        return new AiTraceEntity
        {
            Id = trace.Id,
            TraceId = trace.TraceId,
            SpanId = trace.SpanId,
            StartTime = trace.StartTime,
            EndTime = trace.EndTime,
            Status = (int)trace.Status,
            ErrorCategory = trace.ErrorCategory.HasValue ? (int)trace.ErrorCategory.Value : null,
            ErrorMessage = trace.ErrorMessage,
            UserId = trace.UserId,
            UserName = trace.UserName,
            EntityId = trace.EntityId,
            EntityType = trace.EntityType,
            OperationType = trace.OperationType,
            ProfileId = trace.ProfileId,
            ProfileAlias = trace.ProfileAlias,
            ProviderId = trace.ProviderId,
            ModelId = trace.ModelId,
            FeatureType = trace.FeatureType,
            FeatureId = trace.FeatureId,
            InputTokens = trace.InputTokens,
            OutputTokens = trace.OutputTokens,
            TotalTokens = trace.TotalTokens,
            PromptSnapshot = trace.PromptSnapshot,
            ResponseSnapshot = trace.ResponseSnapshot,
            DetailLevel = (int)trace.DetailLevel,
            Spans = trace.Spans.Select(AiExecutionSpanFactory.BuildEntity).ToList()
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiTraceEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="trace">The domain model with updated values.</param>
    public static void UpdateEntity(AiTraceEntity entity, AiTrace trace)
    {
        entity.TraceId = trace.TraceId;
        entity.SpanId = trace.SpanId;
        entity.StartTime = trace.StartTime;
        entity.EndTime = trace.EndTime;
        entity.Status = (int)trace.Status;
        entity.ErrorCategory = trace.ErrorCategory.HasValue ? (int)trace.ErrorCategory.Value : null;
        entity.ErrorMessage = trace.ErrorMessage;
        entity.UserId = trace.UserId;
        entity.UserName = trace.UserName;
        entity.EntityId = trace.EntityId;
        entity.EntityType = trace.EntityType;
        entity.OperationType = trace.OperationType;
        entity.ProfileId = trace.ProfileId;
        entity.ProfileAlias = trace.ProfileAlias;
        entity.ProviderId = trace.ProviderId;
        entity.ModelId = trace.ModelId;
        entity.FeatureType = trace.FeatureType;
        entity.FeatureId = trace.FeatureId;
        entity.InputTokens = trace.InputTokens;
        entity.OutputTokens = trace.OutputTokens;
        entity.TotalTokens = trace.TotalTokens;
        entity.PromptSnapshot = trace.PromptSnapshot;
        entity.ResponseSnapshot = trace.ResponseSnapshot;
        entity.DetailLevel = (int)trace.DetailLevel;
    }
}
