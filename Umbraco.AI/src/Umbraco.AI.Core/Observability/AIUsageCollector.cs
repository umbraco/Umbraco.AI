using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Thread-safe accumulator of token usage for the AI calls made within an <see cref="AIUsageCollectionScope"/>.
/// Calls are grouped by capability, provider, model, profile and feature. Tool calls may run concurrently,
/// so all access is guarded by a lock.
/// </summary>
internal sealed class AIUsageCollector
{
    private readonly object _lock = new();
    private readonly Dictionary<GroupKey, Group> _groups = new();

    /// <summary>
    /// Records one AI call. A null <paramref name="usage"/>, or one with no token counts at all,
    /// counts the call as unreported and adds no tokens (unknown is not zero). Unknown provider or model stays null.
    /// The call's <paramref name="durationMs"/> is always added, and a call that did not succeed counts as failed
    /// (a failed call still counts towards the call count, and as unreported when it had no usage).
    /// </summary>
    public void RecordCall(
        AICapability capability,
        string? providerId,
        string? modelId,
        Guid? profileId,
        string? profileAlias,
        string? featureType,
        Guid? featureId,
        string? featureAlias,
        UsageDetails? usage,
        long durationMs,
        bool succeeded)
    {
        var key = new GroupKey(capability, providerId, modelId, profileId, featureType, featureId);

        lock (_lock)
        {
            if (!_groups.TryGetValue(key, out var group))
            {
                group = new Group();
                _groups[key] = group;
            }

            group.ProfileAlias ??= profileAlias;
            group.FeatureAlias ??= featureAlias;
            group.CallCount++;
            group.DurationMs += Math.Max(0, durationMs);

            if (!succeeded)
            {
                group.FailedCallCount++;
            }

            if (usage is null
                || (usage.InputTokenCount is null && usage.OutputTokenCount is null && usage.TotalTokenCount is null))
            {
                group.UnreportedCallCount++;
                return;
            }

            var input = usage.InputTokenCount ?? 0;
            var output = usage.OutputTokenCount ?? 0;

            group.InputTokens += input;
            group.OutputTokens += output;
            group.TotalTokens += usage.TotalTokenCount ?? input + output;
        }
    }

    /// <summary>
    /// Returns an immutable snapshot of everything recorded so far.
    /// </summary>
    public AIUsageCollectorSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            var breakdown = _groups
                .Select(pair => new AIUsageCollectorEntry(
                    Capability: pair.Key.Capability,
                    ProviderId: pair.Key.ProviderId,
                    ModelId: pair.Key.ModelId,
                    ProfileId: pair.Key.ProfileId,
                    ProfileAlias: pair.Value.ProfileAlias,
                    FeatureType: pair.Key.FeatureType,
                    FeatureId: pair.Key.FeatureId,
                    FeatureAlias: pair.Value.FeatureAlias,
                    InputTokens: ClampToInt(pair.Value.InputTokens),
                    OutputTokens: ClampToInt(pair.Value.OutputTokens),
                    TotalTokens: ClampToInt(pair.Value.TotalTokens),
                    CallCount: pair.Value.CallCount,
                    UnreportedCallCount: pair.Value.UnreportedCallCount,
                    DurationMs: pair.Value.DurationMs,
                    FailedCallCount: pair.Value.FailedCallCount))
                .OrderBy(e => e.Capability)
                .ThenBy(e => e.ProviderId, StringComparer.Ordinal)
                .ThenBy(e => e.ModelId, StringComparer.Ordinal)
                .ThenBy(e => e.ProfileId)
                .ThenBy(e => e.FeatureType, StringComparer.Ordinal)
                .ThenBy(e => e.FeatureId)
                .ToList();

            return new AIUsageCollectorSnapshot(
                ClampToInt(_groups.Values.Sum(g => g.InputTokens)),
                ClampToInt(_groups.Values.Sum(g => g.OutputTokens)),
                ClampToInt(_groups.Values.Sum(g => g.TotalTokens)),
                _groups.Values.Sum(g => g.CallCount),
                _groups.Values.Sum(g => g.UnreportedCallCount),
                _groups.Values.Sum(g => g.DurationMs),
                _groups.Values.Sum(g => g.FailedCallCount),
                breakdown);
        }
    }

    private static int ClampToInt(long value) => (int)Math.Clamp(value, 0, int.MaxValue);

    private readonly record struct GroupKey(AICapability Capability, string? ProviderId, string? ModelId, Guid? ProfileId, string? FeatureType, Guid? FeatureId);

    private sealed class Group
    {
        public string? ProfileAlias { get; set; }
        public string? FeatureAlias { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }
        public long TotalTokens { get; set; }
        public int CallCount { get; set; }
        public int UnreportedCallCount { get; set; }
        public long DurationMs { get; set; }
        public int FailedCallCount { get; set; }
    }
}
