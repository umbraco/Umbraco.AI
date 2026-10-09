using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Immutable per-breakdown-entry usage totals within an <see cref="AIUsageCollectorSnapshot"/>.
/// </summary>
internal sealed record AIUsageCollectorEntry(
    AICapability Capability,
    string? ProviderId,
    string? ModelId,
    Guid? ProfileId,
    string? ProfileAlias,
    string? FeatureType,
    Guid? FeatureId,
    string? FeatureAlias,
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    int CallCount,
    int UnreportedCallCount,
    long DurationMs,
    int FailedCallCount);
