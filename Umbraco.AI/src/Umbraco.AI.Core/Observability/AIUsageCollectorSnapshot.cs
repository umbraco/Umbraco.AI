namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Immutable point-in-time view of an <see cref="AIUsageCollector"/>.
/// </summary>
internal sealed record AIUsageCollectorSnapshot(
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    int CallCount,
    int UnreportedCallCount,
    long DurationMs,
    int FailedCallCount,
    IReadOnlyList<AIUsageCollectorEntry> Breakdown);
