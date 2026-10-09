using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// How the AI usage of a comparison run changed from its baseline run. Every change is
/// comparison minus baseline, so a positive value means the comparison run used more.
/// See <see cref="AITestRunComparison.UsageComparison"/>, which is null when either run has no usage.
/// </summary>
/// <remarks>
/// Covers the tracked AI calls made by the test feature (grader calls excluded), as recorded on
/// <see cref="AITestOutcome.Usage"/>. <see cref="CallDurationChangeMs"/> is the change in summed AI call
/// time, not in overall run time; see <see cref="AITestRunComparison.DurationChangeMs"/> for that.
/// </remarks>
public sealed class AITestUsageComparison
{
    /// <summary>
    /// Change in input tokens.
    /// </summary>
    public int InputTokensChange { get; set; }

    /// <summary>
    /// Change in output tokens.
    /// </summary>
    public int OutputTokensChange { get; set; }

    /// <summary>
    /// Change in total tokens.
    /// </summary>
    public int TotalTokensChange { get; set; }

    /// <summary>
    /// Change in the number of tracked AI calls.
    /// </summary>
    public int CallCountChange { get; set; }

    /// <summary>
    /// Change in the number of failed AI calls.
    /// </summary>
    public int FailedCallCountChange { get; set; }

    /// <summary>
    /// Change in summed AI call time, in milliseconds (positive = more AI time). Overlapping calls are
    /// summed, so this is AI time, not wall-clock time, and it excludes grading.
    /// </summary>
    public long CallDurationChangeMs { get; set; }

    /// <summary>
    /// Whether either run had calls that reported no usage. When true, the token totals of that run
    /// are a lower bound, so the token changes are approximate.
    /// </summary>
    public bool HasUnreportedCalls { get; set; }

    /// <summary>
    /// Whether the runs used a different set of capabilities, providers, models, profiles or features,
    /// i.e. whether any <see cref="Entries"/> item exists in only one run. Explains token changes that
    /// come from switching models or features rather than from the same calls costing more.
    /// </summary>
    public bool BreakdownChanged { get; set; }

    /// <summary>
    /// Per-entry comparison, one item per capability, provider, model, profile and feature combination
    /// found in either run's <see cref="AITestUsage.Breakdown"/>. Empty when neither run recorded a breakdown.
    /// </summary>
    public IReadOnlyList<AITestUsageEntryComparison> Entries { get; set; } = [];
}

/// <summary>
/// Comparison of one usage breakdown entry (a capability, provider, model, profile and feature combination)
/// between a baseline run and a comparison run. Either side is null when the combination only appears in
/// the other run, for example when the comparison run switched to a different model.
/// </summary>
public sealed class AITestUsageEntryComparison
{
    /// <summary>
    /// The capability the calls were made with.
    /// </summary>
    public AICapability Capability { get; set; }

    /// <summary>
    /// The ID of the provider that served the calls, if known.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// The ID of the model that served the calls, if known.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// The ID of the profile the calls resolved to, if any.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// The alias of the profile the calls resolved to, if any.
    /// </summary>
    public string? ProfileAlias { get; set; }

    /// <summary>
    /// The type of feature that made the calls, if any.
    /// </summary>
    public string? FeatureType { get; set; }

    /// <summary>
    /// The ID of the feature that made the calls, if any.
    /// </summary>
    public Guid? FeatureId { get; set; }

    /// <summary>
    /// The alias of the feature that made the calls, if any.
    /// </summary>
    public string? FeatureAlias { get; set; }

    /// <summary>
    /// The entry in the baseline run, or null when only the comparison run has it.
    /// </summary>
    public AITestUsageEntry? BaselineEntry { get; set; }

    /// <summary>
    /// The entry in the comparison run, or null when only the baseline run has it.
    /// </summary>
    public AITestUsageEntry? ComparisonEntry { get; set; }

    /// <summary>
    /// Change in total tokens for this entry. A missing side counts as zero.
    /// </summary>
    public int TotalTokensChange { get; set; }

    /// <summary>
    /// Change in summed AI call time for this entry, in milliseconds. A missing side counts as zero.
    /// </summary>
    public long CallDurationChangeMs { get; set; }

    /// <summary>
    /// Change in failed calls for this entry. A missing side counts as zero.
    /// </summary>
    public int FailedCallCountChange { get; set; }
}
