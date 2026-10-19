namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Tracks an AI operation, for every capability: reads the call's identity once at the start, times it,
/// decides how it ended, and hands start and outcome to each <see cref="IAIOperationRecorder"/> (audit
/// log, trace tags, usage analytics, test-run usage). What gets recorded lives in the recorders.
/// </summary>
internal interface IAIOperationTracker
{
    /// <summary>Runs <paramref name="operation"/> as a tracked call (non-streaming path).</summary>
    Task<AITrackedOperationResult<TResult>> TrackAsync<TResult>(
        AIOperationDescriptor descriptor,
        Func<CancellationToken, Task<AITrackedOperationResult<TResult>>> operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Starts a tracked call and returns a scope the caller enters around the work and then completes or
    /// fails. For streaming operations, where the result is only known after enumeration.
    /// </summary>
    Task<AIOperationScope> BeginAsync(AIOperationDescriptor descriptor, CancellationToken cancellationToken);
}
