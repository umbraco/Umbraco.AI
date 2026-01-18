using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Factory for creating AiUsageRecord instances with proper validation and user context capture.
/// </summary>
internal sealed class AiUsageRecordFactory : IAiUsageRecordFactory
{
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly ILogger<AiUsageRecordFactory> _logger;

    public AiUsageRecordFactory(
        IOptionsMonitor<AiAnalyticsOptions> options,
        IBackOfficeSecurityAccessor securityAccessor,
        ILogger<AiUsageRecordFactory> logger)
    {
        _options = options;
        _securityAccessor = securityAccessor;
        _logger = logger;
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <inheritdoc />
    public AiUsageRecord Create(
        AiUsageRecordContext context,
        AiUsageRecordResult result)
    {
        // Validate required context fields
        if (context.ProfileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId must be set in context", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.ProfileAlias))
        {
            throw new ArgumentException("ProfileAlias must be set in context", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.ProviderId))
        {
            throw new ArgumentException("ProviderId must be set in context", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.ModelId))
        {
            throw new ArgumentException("ModelId must be set in context", nameof(context));
        }

        // Validate numeric ranges
        if (result.Usage?.InputTokenCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(result.Usage.InputTokenCount), result.Usage.InputTokenCount, "Input tokens must be non-negative");
        }

        if (result.Usage?.OutputTokenCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(result.Usage.OutputTokenCount), result.Usage?.OutputTokenCount, "Output tokens must be non-negative");
        }

        if (result.Usage?.TotalTokenCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(result.Usage.TotalTokenCount), result.Usage?.TotalTokenCount, "Total tokens must be non-negative");
        }

        if (result.DurationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(result.DurationMs), result.DurationMs, "Duration must be non-negative");
        }

        // Capture user context based on options
        var user = _securityAccessor.BackOfficeSecurity?.CurrentUser;
        var includeUser = _options.CurrentValue.IncludeUsageUserDimension;

        var timestamp = DateTime.UtcNow;

        var record = new AiUsageRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp,
            Capability = context.Capability,
            UserId = includeUser ? user?.Key.ToString() : null,
            UserName = includeUser ? user?.Name : null,
            ProfileId = context.ProfileId,
            ProfileAlias = context.ProfileAlias,
            ProviderId = context.ProviderId,
            ModelId = context.ModelId,
            FeatureType = _options.CurrentValue.IncludeUsageFeatureTypeDimension ? context.FeatureType : null,
            FeatureId = _options.CurrentValue.IncludeUsageFeatureTypeDimension ? context.FeatureId : null,
            EntityId = context.EntityId,
            EntityType = _options.CurrentValue.IncludeUsageEntityTypeDimension ? context.EntityType : null,
            InputTokens = result.Usage?.InputTokenCount ?? 0,
            OutputTokens = result.Usage?.OutputTokenCount ?? 0,
            TotalTokens = result.Usage?.TotalTokenCount ?? 0,
            DurationMs = result.DurationMs,
            Status = result.Succeeded ? "Succeeded" : "Failed",
            ErrorMessage = result.ErrorMessage,
            CreatedAt = timestamp
        };

        _logger.LogDebug(
            "Created usage record {RecordId} for {Capability} operation (Profile: {ProfileAlias}, Provider: {ProviderId}, Model: {ModelId})",
            record.Id,
            record.Capability,
            record.ProfileAlias,
            record.ProviderId,
            record.ModelId);

        return record;
    }
}
