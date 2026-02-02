using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Persistence.Analytics.Usage;

/// <summary>
/// Factory for mapping between usage analytics domain models and database entities.
/// </summary>
internal static class AIUsageRecordFactory
{
    /// <summary>
    /// Creates an <see cref="AIUsageRecord"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AIUsageRecord BuildUsageRecordDomain(AIUsageRecordEntity entity)
    {
        return new AIUsageRecord
        {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            Capability = (AICapability)entity.Capability,
            UserId = entity.UserId,
            UserName = entity.UserName,
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            FeatureType = entity.FeatureType,
            FeatureId = entity.FeatureId,
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            DurationMs = entity.DurationMs,
            Status = entity.Status,
            ErrorMessage = entity.ErrorMessage,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Creates an <see cref="AIUsageRecordEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="record">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AIUsageRecordEntity BuildUsageRecordEntity(AIUsageRecord record)
    {
        return new AIUsageRecordEntity
        {
            Id = record.Id,
            Timestamp = record.Timestamp,
            Capability = (int)record.Capability,
            UserId = record.UserId,
            UserName = record.UserName,
            ProfileId = record.ProfileId,
            ProfileAlias = record.ProfileAlias,
            ProviderId = record.ProviderId,
            ModelId = record.ModelId,
            FeatureType = record.FeatureType,
            FeatureId = record.FeatureId,
            EntityId = record.EntityId,
            EntityType = record.EntityType,
            InputTokens = record.InputTokens,
            OutputTokens = record.OutputTokens,
            TotalTokens = record.TotalTokens,
            DurationMs = record.DurationMs,
            Status = record.Status,
            ErrorMessage = record.ErrorMessage,
            CreatedAt = record.CreatedAt
        };
    }

    /// <summary>
    /// Creates an <see cref="AIUsageStatistics"/> domain model from a hourly database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AIUsageStatistics BuildStatisticsDomain(AIUsageStatisticsHourlyEntity entity)
    {
        return new AIUsageStatistics
        {
            Id = entity.Id,
            Period = entity.Period,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            Capability = (AICapability)entity.Capability,
            UserId = entity.UserId,
            UserName = entity.UserName,
            EntityType = entity.EntityType,
            FeatureType = entity.FeatureType,
            RequestCount = entity.RequestCount,
            SuccessCount = entity.SuccessCount,
            FailureCount = entity.FailureCount,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            TotalDurationMs = entity.TotalDurationMs,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Creates an <see cref="AIUsageStatistics"/> domain model from a daily database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AIUsageStatistics BuildStatisticsDomain(AIUsageStatisticsDailyEntity entity)
    {
        return new AIUsageStatistics
        {
            Id = entity.Id,
            Period = entity.Period,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            Capability = (AICapability)entity.Capability,
            UserId = entity.UserId,
            UserName = entity.UserName,
            EntityType = entity.EntityType,
            FeatureType = entity.FeatureType,
            RequestCount = entity.RequestCount,
            SuccessCount = entity.SuccessCount,
            FailureCount = entity.FailureCount,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            TotalDurationMs = entity.TotalDurationMs,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Creates an <see cref="AIUsageStatisticsHourlyEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="statistics">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AIUsageStatisticsHourlyEntity BuildHourlyStatisticsEntity(AIUsageStatistics statistics)
    {
        return new AIUsageStatisticsHourlyEntity
        {
            Id = statistics.Id,
            Period = statistics.Period,
            ProviderId = statistics.ProviderId,
            ModelId = statistics.ModelId,
            ProfileId = statistics.ProfileId,
            ProfileAlias = statistics.ProfileAlias,
            Capability = (int)statistics.Capability,
            UserId = statistics.UserId,
            UserName = statistics.UserName,
            EntityType = statistics.EntityType,
            FeatureType = statistics.FeatureType,
            RequestCount = statistics.RequestCount,
            SuccessCount = statistics.SuccessCount,
            FailureCount = statistics.FailureCount,
            InputTokens = statistics.InputTokens,
            OutputTokens = statistics.OutputTokens,
            TotalTokens = statistics.TotalTokens,
            TotalDurationMs = statistics.TotalDurationMs,
            CreatedAt = statistics.CreatedAt
        };
    }

    /// <summary>
    /// Creates an <see cref="AIUsageStatisticsDailyEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="statistics">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AIUsageStatisticsDailyEntity BuildDailyStatisticsEntity(AIUsageStatistics statistics)
    {
        return new AIUsageStatisticsDailyEntity
        {
            Id = statistics.Id,
            Period = statistics.Period,
            ProviderId = statistics.ProviderId,
            ModelId = statistics.ModelId,
            ProfileId = statistics.ProfileId,
            ProfileAlias = statistics.ProfileAlias,
            Capability = (int)statistics.Capability,
            UserId = statistics.UserId,
            UserName = statistics.UserName,
            EntityType = statistics.EntityType,
            FeatureType = statistics.FeatureType,
            RequestCount = statistics.RequestCount,
            SuccessCount = statistics.SuccessCount,
            FailureCount = statistics.FailureCount,
            InputTokens = statistics.InputTokens,
            OutputTokens = statistics.OutputTokens,
            TotalTokens = statistics.TotalTokens,
            TotalDurationMs = statistics.TotalDurationMs,
            CreatedAt = statistics.CreatedAt
        };
    }
}
