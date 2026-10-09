using Umbraco.AI.Core.Utilities;

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
/// Work running in parallel (e.g. two AI calls started together) each sees its own context, never one the
/// other made current, and work keeps the context it started with for its whole life (see
/// <see cref="AIAmbientStack{T}"/>). Streaming entry points re-enter the scope around each step of the stream
/// they run (see <see cref="AIRuntimeContextStreamExtensions"/>).
/// </para>
/// </remarks>
internal sealed class AIRuntimeContextScopeProvider : IAIRuntimeContextScopeProvider, IAIRuntimeContextAccessor
{
    private static readonly AIAmbientStack<Scope> Scopes = new();

    /// <summary>
    /// The scope current in this async flow, if any.
    /// </summary>
    internal static IAIRuntimeContextScope? CurrentScope => Scopes.Current;

    /// <inheritdoc />
    public AIRuntimeContext? Context => Scopes.Current?.Context;

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope()
        => CreateScope([]);

    /// <inheritdoc />
    public IAIRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items)
    {
        var scope = new Scope(new AIRuntimeContext(items), Scopes.Current);
        scope.Entry = Scopes.Push(scope);
        return scope;
    }

    /// <summary>
    /// Makes <paramref name="scope"/> current until the returned handle is disposed, for code that runs where
    /// the scope's own flow doesn't reach (the steps of a stream enumerated by a caller).
    /// </summary>
    internal static IDisposable? Enter(IAIRuntimeContextScope? scope)
        => scope is Scope entered ? Scopes.Enter(entered.Entry) : null;

    /// <summary>
    /// A context made current by <see cref="CreateScope(IEnumerable{AIRequestContextItem})"/>, with its depth and
    /// parent fixed when it is created.
    /// </summary>
    private sealed class Scope(AIRuntimeContext context, Scope? parent) : IAIRuntimeContextScope
    {
        public AIRuntimeContext Context { get; } = context;

        public AIRuntimeContext? ParentContext { get; } = parent?.Context;

        public int Depth { get; } = (parent?.Depth ?? 0) + 1;

        public AIAmbientStack<Scope>.Entry Entry { get; set; } = null!;

        public void Dispose() => Entry.Dispose();
    }
}
