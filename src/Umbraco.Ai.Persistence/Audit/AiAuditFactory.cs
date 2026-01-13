using Umbraco.Ai.Core.Audit;

namespace Umbraco.Ai.Persistence.Audit;

/// <summary>
/// Factory for mapping between <see cref="AiAudit"/> domain models and <see cref="AiAuditEntity"/> database entities.
/// </summary>
internal static class AiAuditFactory
{
    /// <summary>
    /// Creates an <see cref="AiAudit"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiAudit BuildDomain(AiAuditEntity entity)
    {
        var activities = entity.Activities
            .Select(AiAuditActivityFactory.BuildDomain)
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        return new AiAudit
        {
            Id = entity.Id,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Status = (AiAuditStatus)entity.Status,
            ErrorCategory = entity.ErrorCategory.HasValue ? (AiAuditErrorCategory)entity.ErrorCategory.Value : null,
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
            DetailLevel = (AiAuditDetailLevel)entity.DetailLevel,
            Activities = activities
        };
    }

    /// <summary>
    /// Creates an <see cref="AiAuditEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="trace">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiAuditEntity BuildEntity(AiAudit trace)
    {
        return new AiAuditEntity
        {
            Id = trace.Id,
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
            Activities = trace.Activities.Select(AiAuditActivityFactory.BuildEntity).ToList()
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiAuditEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="trace">The domain model with updated values.</param>
    public static void UpdateEntity(AiAuditEntity entity, AiAudit trace)
    {
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
