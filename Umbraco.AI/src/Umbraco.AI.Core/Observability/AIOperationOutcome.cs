using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// How a tracked call ended, measured once by the tracker and handed to every <see cref="IAIOperationRecording"/>.
/// </summary>
/// <param name="Status">How the call ended.</param>
/// <param name="Usage">Token usage reported by the provider, if any. A failed call can still carry partial usage.</param>
/// <param name="DurationMs">
/// How long the call took, leaving out the time tracked calls nested in it were running, so a judge or
/// embedding call's time is counted once, in its own outcome (#563).
/// </param>
/// <param name="Exception">The failure, when the call failed.</param>
/// <param name="ResponseData">
/// What the call returned (see <see cref="AITrackedOperationResult{TResult}.ResponseData"/>). Null when the
/// call failed.
/// </param>
internal sealed record AIOperationOutcome(
    AIOperationStatus Status,
    UsageDetails? Usage,
    long DurationMs,
    Exception? Exception,
    object? ResponseData = null)
{
    /// <summary>Whether the call succeeded.</summary>
    public bool Succeeded => Status == AIOperationStatus.Succeeded;
}

/// <summary>
/// How a tracked call ended.
/// </summary>
internal enum AIOperationStatus
{
    /// <summary>The call completed.</summary>
    Succeeded,

    /// <summary>The call threw, or ended on a provider error.</summary>
    Failed,

    /// <summary>A guardrail stopped the call (<see cref="Guardrails.AIGuardrailBlockedException"/>).</summary>
    Blocked,
}
