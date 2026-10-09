namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Provides runtime context scope management, with the current context held per async flow.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/> makes a new context current for the
/// code that created it and everything it awaits or starts from there. Disposing the scope makes the previous
/// context current again.
/// </para>
/// <para>
/// The current context is held in an <see cref="AsyncLocal{T}"/>, so work running in parallel (e.g. two AI
/// calls started together) each see their own context, never one the other made current. The flip side is
/// that a context made current inside an async method, including an async iterator between its yields, is
/// not current for its caller. Code that creates a scope inside an async iterator re-enters it around each
/// step of the stream it runs (see <see cref="AIRuntimeContextStreamExtensions"/>).
/// </para>
/// </remarks>
internal sealed class AIRuntimeContextScopeProvider : IAIRuntimeContextScopeProvider, IAIRuntimeContextAccessor
{
    private static readonly AsyncLocal<Scope?> CurrentScope = new();

    /// <inheritdoc />
    public AIRuntimeContext? Context => Scope.Live(CurrentScope.Value)?.Context;

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope()
        => CreateScope([]);

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items)
    {
        var scope = new Scope(new AIRuntimeContext(items), Scope.Live(CurrentScope.Value));
        CurrentScope.Value = scope;
        return scope;
    }

    /// <summary>
    /// Makes <paramref name="scope"/> current until the returned handle is disposed, for code that runs where
    /// the scope's own flow doesn't reach (the steps of a stream enumerated by a caller).
    /// </summary>
    internal static IDisposable? Enter(IAIRuntimeContextScope? scope)
    {
        if (scope is not Scope { IsDisposed: false } entered)
        {
            return null;
        }

        var previous = CurrentScope.Value;
        CurrentScope.Value = entered;
        return new Restore(previous);
    }

    private sealed class Restore(Scope? previous) : IDisposable
    {
        public void Dispose() => CurrentScope.Value = previous;
    }

    /// <summary>
    /// A context made current by <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/>, linked to the
    /// scope that was current when it was created.
    /// </summary>
    private sealed class Scope : IAIRuntimeContextScope
    {
        private readonly Scope? _parent;
        private volatile bool _disposed;

        public Scope(AIRuntimeContext context, Scope? parent)
        {
            Context = context;
            _parent = parent;
        }

        public AIRuntimeContext Context { get; }

        public AIRuntimeContext? ParentContext => Live(_parent)?.Context;

        public int Depth
        {
            get
            {
                var depth = 0;
                for (var scope = Live(this); scope is not null; scope = Live(scope._parent))
                {
                    depth++;
                }

                return depth;
            }
        }

        public bool IsDisposed => _disposed;

        /// <summary>
        /// The nearest scope from <paramref name="scope"/> up that hasn't been disposed.
        /// </summary>
        /// <remarks>
        /// Scopes can be disposed out of order, or after work they started (e.g. a background task) took a copy
        /// of the current scope, so a disposed scope is skipped rather than trusted.
        /// </remarks>
        public static Scope? Live(Scope? scope)
        {
            while (scope is { _disposed: true })
            {
                scope = scope._parent;
            }

            return scope;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Only changes the current scope for the flow disposing it; any other flow skips it as disposed.
            if (ReferenceEquals(CurrentScope.Value, this))
            {
                CurrentScope.Value = Live(_parent);
            }
        }
    }
}
