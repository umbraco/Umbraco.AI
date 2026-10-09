using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAIContextAccessor"/>, with the current context held per async flow.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SetContext"/> makes a resolved context current for the code that set it and everything it awaits
/// or starts (e.g. the tools run by the function-invoking client inside the call). Disposing the handle makes
/// the context that was current before current again, for the code that disposes it (see
/// <see cref="AIAmbientStack{T}"/>).
/// </para>
/// <para>
/// So each AI call's tools see that call's resources: AI calls running at the same time in one request, or a
/// nested call made by a tool, never see or clear another call's context, and it works with no HTTP request
/// (e.g. background jobs). The context injection middleware re-enters the context around each step of a
/// stream, since a context made current inside an async iterator is no longer current once it yields.
/// </para>
/// </remarks>
internal sealed class AIContextAccessor : IAIContextAccessor
{
    private static readonly AIAmbientStack<AIResolvedContext> Contexts = new();

    /// <inheritdoc />
    public AIResolvedContext? Context => Contexts.Current;

    /// <inheritdoc />
    public IDisposable SetContext(AIResolvedContext context) => Contexts.Push(context);

    /// <summary>
    /// Makes the context set by <paramref name="handle"/> current until the returned handle is disposed, for code
    /// that runs where the setter's own flow doesn't reach (the steps of a stream enumerated by a caller).
    /// </summary>
    internal static IDisposable? Enter(IDisposable? handle) => Contexts.Enter(handle);
}
