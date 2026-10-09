namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Provides runtime context scope management, with the current context held per async flow.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/> makes a new context current for the
/// code that created it and everything it awaits or starts from there. Disposing the scope makes the context
/// that was current when it was created current again, for the code that disposes it.
/// </para>
/// <para>
/// The current context is held in an <see cref="AsyncLocal{T}"/>, so work running in parallel (e.g. two AI
/// calls started together) each see their own context, never one the other made current. Work keeps the
/// context it started with for its whole life, even after the scope's owner disposes it (e.g. a background
/// task that outlives the call that started it), so it always sees the same context, whatever the timing.
/// </para>
/// <para>
/// A context made current inside an async method, including an async iterator between its yields, is not
/// current for its caller. Streaming entry points re-enter the scope around each step of the stream they run
/// (see <see cref="AIRuntimeContextStreamExtensions"/>).
/// </para>
/// </remarks>
internal sealed class AIRuntimeContextScopeProvider : IAIRuntimeContextScopeProvider, IAIRuntimeContextAccessor
{
    private static readonly AsyncLocal<Scope?> CurrentNode = new();

    /// <summary>
    /// The scope current in this async flow, if any.
    /// </summary>
    internal static IAIRuntimeContextScope? CurrentScope => CurrentNode.Value;

    /// <inheritdoc />
    public AIRuntimeContext? Context => CurrentNode.Value?.Context;

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope()
        => CreateScope([]);

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items)
    {
        var scope = new Scope(new AIRuntimeContext(items), CurrentNode.Value);
        CurrentNode.Value = scope;
        return scope;
    }

    /// <summary>
    /// Makes <paramref name="scope"/> current until the returned handle is disposed, for code that runs where
    /// the scope's own flow doesn't reach (the steps of a stream enumerated by a caller).
    /// </summary>
    internal static IDisposable? Enter(IAIRuntimeContextScope? scope)
    {
        if (scope is not Scope entered)
        {
            return null;
        }

        var previous = CurrentNode.Value;
        CurrentNode.Value = entered;
        return new Restore(previous);
    }

    private sealed class Restore(Scope? previous) : IDisposable
    {
        public void Dispose() => CurrentNode.Value = previous;
    }

    /// <summary>
    /// A context made current by <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/>, linked to the
    /// scope that was current when it was created.
    /// </summary>
    private sealed class Scope : IAIRuntimeContextScope
    {
        private readonly Scope? _parent;
        private bool _disposed;

        public Scope(AIRuntimeContext context, Scope? parent)
        {
            Context = context;
            _parent = parent;
            ParentContext = parent?.Context;
            Depth = (parent?.Depth ?? 0) + 1;
        }

        public AIRuntimeContext Context { get; }

        public AIRuntimeContext? ParentContext { get; }

        public int Depth { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Restores the scope that was current when this one was created, for the flow disposing it only.
            // Work in other flows that has this scope keeps it. Disposing scopes out of order (the outer one
            // first) is a usage error; like Activity.Current, nothing is repaired.
            if (ReferenceEquals(CurrentNode.Value, this))
            {
                CurrentNode.Value = _parent;
            }
        }
    }
}
