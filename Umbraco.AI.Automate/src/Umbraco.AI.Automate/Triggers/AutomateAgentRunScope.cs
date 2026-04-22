namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Ambient marker used to detect when an AI agent run is being executed from within
/// an Automate workflow action (typically <c>RunAgentAction</c>). The agent run triggers
/// check this to suppress themselves and prevent unbounded recursion.
/// </summary>
/// <remarks>
/// We cannot rely on <c>Umbraco.Automate.Core.Execution.IExecutionContextAccessor</c> here:
/// that accessor's <c>AsyncLocal</c> is only populated during the very narrow window in which
/// <c>AutomationExecutor</c> calls <c>StartWorkflow</c>. Once WorkflowCore picks up the
/// workflow and invokes a step body on its own scheduler, the accessor returns null —
/// the execution context is handed to actions via <c>ActionContext.ExecutionContext</c>
/// instead, which the accessor does not see.
///
/// This scope is set by the calling action just before invoking the agent service and flows
/// through <c>await</c> boundaries (including the notification publish in the service's
/// <c>finally</c> block) because <c>PublishAsync</c> dispatches handlers synchronously on
/// the same async flow.
/// </remarks>
internal static class AutomateAgentRunScope
{
    private static readonly AsyncLocal<bool> Current = new();

    /// <summary>
    /// Gets a value indicating whether the current async flow is inside an Automate
    /// workflow-driven agent run.
    /// </summary>
    public static bool IsActive => Current.Value;

    /// <summary>
    /// Enters a new scope. Callers must dispose the returned handle when the agent run
    /// completes (before the enclosing async flow unwinds). Nested scopes are safe:
    /// disposing an inner scope restores the outer scope's state rather than clearing.
    /// </summary>
    public static IDisposable Enter()
    {
        var previous = Current.Value;
        Current.Value = true;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly bool _previous;
        private bool _disposed;

        public Scope(bool previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Current.Value = _previous;
        }
    }
}
