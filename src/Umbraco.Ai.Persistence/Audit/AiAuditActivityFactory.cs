using Umbraco.Ai.Core.Audit;

namespace Umbraco.Ai.Persistence.Audit;

/// <summary>
/// Factory for mapping between <see cref="AiAuditActivity"/> domain models and <see cref="AiAuditActivityEntity"/> database entities.
/// </summary>
internal static class AiAuditActivityFactory
{
    /// <summary>
    /// Creates an <see cref="AiAuditActivity"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiAuditActivity BuildDomain(AiAuditActivityEntity entity)
    {
        return new AiAuditActivity
        {
            Id = entity.Id,
            AuditId = entity.AuditId,
            ActivityName = entity.ActivityName,
            ActivityType = (AiAuditActivityType)entity.ActivityType,
            SequenceNumber = entity.SequenceNumber,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Status = (AiAuditActivityStatus)entity.Status,
            InputData = entity.InputData,
            OutputData = entity.OutputData,
            ErrorData = entity.ErrorData,
            RetryCount = entity.RetryCount,
            TokensUsed = entity.TokensUsed
        };
    }

    /// <summary>
    /// Creates an <see cref="AiAuditActivityEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="span">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiAuditActivityEntity BuildEntity(AiAuditActivity span)
    {
        return new AiAuditActivityEntity
        {
            Id = span.Id,
            AuditId = span.AuditId,
            ActivityName = span.ActivityName,
            ActivityType = (int)span.ActivityType,
            SequenceNumber = span.SequenceNumber,
            StartTime = span.StartTime,
            EndTime = span.EndTime,
            Status = (int)span.Status,
            InputData = span.InputData,
            OutputData = span.OutputData,
            ErrorData = span.ErrorData,
            RetryCount = span.RetryCount,
            TokensUsed = span.TokensUsed
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiAuditActivityEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="span">The domain model with updated values.</param>
    public static void UpdateEntity(AiAuditActivityEntity entity, AiAuditActivity span)
    {
        entity.ActivityName = span.ActivityName;
        entity.ActivityType = (int)span.ActivityType;
        entity.SequenceNumber = span.SequenceNumber;
        entity.StartTime = span.StartTime;
        entity.EndTime = span.EndTime;
        entity.Status = (int)span.Status;
        entity.InputData = span.InputData;
        entity.OutputData = span.OutputData;
        entity.ErrorData = span.ErrorData;
        entity.RetryCount = span.RetryCount;
        entity.TokensUsed = span.TokensUsed;
    }
}
