namespace Umbraco.AI.Core.Tests;

/// <summary>
/// AI usage for a test execution: tokens, call counts (total, unreported and failed), the summed duration of
/// the AI calls (not wall-clock; overlapping calls are summed), and a <see cref="Breakdown"/> per
/// capability, provider, model, profile and feature. Covers the tracked AI calls made by the test feature;
/// grader calls are excluded. See <see cref="AITestOutcome.Usage"/>, which is null when the run made
/// no tracked call.
/// </summary>
public sealed class AITestUsage
{
    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Number of tracked AI calls made during the run, including calls that reported no usage.
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// Number of tracked calls that returned no usage details. When greater than zero, the token
    /// totals are a lower bound because those calls contributed nothing to them.
    /// </summary>
    public int UnreportedCallCount { get; set; }

    /// <summary>
    /// Sum of the durations, in milliseconds, of every tracked AI call in the run. Calls that overlap
    /// (for example parallel tool calls) are summed, so this is AI time, not wall-clock time.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Number of tracked calls that failed. Failed calls are included in <see cref="CallCount"/>.
    /// </summary>
    public int FailedCallCount { get; set; }

    /// <summary>
    /// Breakdown of the usage, one entry per capability, provider, model, profile and feature.
    /// Graders can sum just the entries they care about (for example only the target prompt's own call,
    /// or only guardrail judge calls). The top-level totals cover every tracked call made by the test feature (grader calls excluded).
    /// Never null; empty when no breakdown was recorded (for example, usage persisted before the breakdown existed).
    /// </summary>
    public List<AITestUsageEntry> Breakdown { get; set; } = [];
}
