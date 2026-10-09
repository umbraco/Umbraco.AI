namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAIContextAccessor"/>, with the current context held per async flow.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SetContext"/> makes a resolved context current for the code that set it and everything it awaits
/// or starts (e.g. the tools run by the function-invoking client inside the call). Disposing the handle makes
/// the context that was current before current again, for the code that disposes it. Work that outlives the
/// call keeps the context it started with.
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
    private static readonly AsyncLocal<Entry?> CurrentEntry = new();

    /// <inheritdoc />
    public AIResolvedContext? Context => CurrentEntry.Value?.Context;

    /// <inheritdoc />
    public IDisposable SetContext(AIResolvedContext context)
    {
        var entry = new Entry(context, CurrentEntry.Value);
        CurrentEntry.Value = entry;
        return entry;
    }

    /// <summary>
    /// Makes the context set by <paramref name="handle"/> current until the returned handle is disposed, for code
    /// that runs where the setter's own flow doesn't reach (the steps of a stream enumerated by a caller).
    /// </summary>
    internal static IDisposable? Enter(IDisposable? handle)
    {
        if (handle is not Entry entry)
        {
            return null;
        }

        var previous = CurrentEntry.Value;
        CurrentEntry.Value = entry;
        return new Restore(previous);
    }

    private sealed class Restore(Entry? previous) : IDisposable
    {
        public void Dispose() => CurrentEntry.Value = previous;
    }

    private sealed class Entry(AIResolvedContext context, Entry? previous) : IDisposable
    {
        private bool _disposed;

        public AIResolvedContext Context { get; } = context;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Restores the context current when this one was set, for the flow disposing it only.
            if (ReferenceEquals(CurrentEntry.Value, this))
            {
                CurrentEntry.Value = previous;
            }
        }
    }
}
