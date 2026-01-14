using Umbraco.Ai.Core.Analytics;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Persistence.Analytics;

/// <summary>
/// Factory for mapping between usage analytics domain models and database entities.
/// </summary>
internal static class AiUsageRecordFactory
{
    /// <summary>
    /// Creates an <see cref="AiUsageRecord"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiUsageRecord BuildUsageRecordDomain(AiUsageRecordEntity entity)
    {
        return new AiUsageRecord
        {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            Capability = (AiCapability)entity.Capability,
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
    /// Creates an <see cref="AiUsageRecordEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="record">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiUsageRecordEntity BuildUsageRecordEntity(AiUsageRecord record)
    {
        return new AiUsageRecordEntity
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
    /// Creates an <see cref="AiUsageStatistics"/> domain model from a hourly database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiUsageStatistics BuildStatisticsDomain(AiUsageStatisticsHourlyEntity entity)
    {
        return new AiUsageStatistics
        {
            Id = entity.Id,
            Period = entity.Period,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            Capability = (AiCapability)entity.Capability,
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
    /// Creates an <see cref="AiUsageStatistics"/> domain model from a daily database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiUsageStatistics BuildStatisticsDomain(AiUsageStatisticsDailyEntity entity)
    {
        return new AiUsageStatistics
        {
            Id = entity.Id,
            Period = entity.Period,
            ProviderId = entity.ProviderId,
            ModelId = entity.ModelId,
            ProfileId = entity.ProfileId,
            ProfileAlias = entity.ProfileAlias,
            Capability = (AiCapability)entity.Capability,
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
    /// Creates an <see cref="AiUsageStatisticsHourlyEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="statistics">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiUsageStatisticsHourlyEntity BuildHourlyStatisticsEntity(AiUsageStatistics statistics)
    {
        return new AiUsageStatisticsHourlyEntity
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
    /// Creates an <see cref="AiUsageStatisticsDailyEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="statistics">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiUsageStatisticsDailyEntity BuildDailyStatisticsEntity(AiUsageStatistics statistics)
    {
        return new AiUsageStatisticsDailyEntity
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
