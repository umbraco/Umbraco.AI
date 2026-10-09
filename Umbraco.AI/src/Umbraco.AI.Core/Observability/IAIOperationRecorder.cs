namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Records something about every tracked AI call: the audit log, trace tags, usage analytics, the
/// test-run usage collector. The tracker measures the call once and hands the result to each recorder
/// in turn; a recorder may drop or reshape it, but never measures anything again.
/// </summary>
/// <remarks>
/// Internal plumbing, not an extension point. Recorders are registered as an ordered list in
/// <c>UmbracoBuilderExtensions</c>, and the tracker calls them in that order.
/// </remarks>
internal interface IAIOperationRecorder
{
    /// <summary>
    /// Called when a tracked call starts. Returns the recording that will receive the call's outcome,
    /// or null to skip this call.
    /// </summary>
    ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken);
}

/// <summary>
/// One recorder's state for a single tracked call, from start to end.
/// </summary>
internal interface IAIOperationRecording
{
    /// <summary>
    /// Opens anything that must be ambient while the call's work runs, such as the audit parent for nested
    /// calls. Returns null when there is nothing to open.
    /// </summary>
    /// <remarks>
    /// The tracker enters it in the caller's own frame, directly around the work (for a stream, around each
    /// step of the inner enumerator): AsyncLocal changes made inside an async method or iterator do not
    /// survive its return or a yield.
    /// </remarks>
    IDisposable? EnterScope() => null;

    /// <summary>
    /// Called once when the call ends, successfully or not. Must not throw into the AI call; the tracker
    /// also guards against it.
    /// </summary>
    ValueTask EndAsync(AIOperationOutcome outcome);
}
