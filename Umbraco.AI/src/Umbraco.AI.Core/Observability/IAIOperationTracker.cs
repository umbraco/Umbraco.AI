namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Capability-agnostic recorder of usage analytics + audit entries around an AI operation.
/// The single source of truth for the extract → record → queue orchestration.
/// </summary>
internal interface IAIOperationTracker
{
    /// <summary>Runs <paramref name="operation"/> with audit + usage recording (non-streaming path).</summary>
    Task<AITrackedOperationResult<TResult>> TrackAsync<TResult>(
        AIOperationDescriptor descriptor,
        Func<CancellationToken, Task<AITrackedOperationResult<TResult>>> operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Starts audit + timing and returns a scope the caller completes/fails. For streaming operations
    /// where the result is only known after enumeration.
    /// </summary>
    Task<AIOperationScope> BeginAsync(AIOperationDescriptor descriptor, CancellationToken cancellationToken);
}
