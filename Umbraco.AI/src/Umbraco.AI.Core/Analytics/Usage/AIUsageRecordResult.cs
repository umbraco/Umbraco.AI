using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// Result details for an AI usage record.
/// Encapsulates metrics and status from an AI operation.
/// </summary>
public sealed class AIUsageRecordResult
{
    /// <summary>
    /// Gets the duration of the operation in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Gets whether a guardrail stopped the operation. Only meaningful when <see cref="Succeeded"/> is false;
    /// the record is then stored as "Blocked" rather than "Failed", and still counts as a failure in totals.
    /// </summary>
    public bool Blocked { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets whether the operation was made while another tracked AI operation was running.
    /// </summary>
    public bool IsNested { get; init; }

    /// <summary>
    /// Gets or sets the token usage information from Microsoft.Extensions.AI.
    /// </summary>
    public UsageDetails? Usage { get; init; }
}
