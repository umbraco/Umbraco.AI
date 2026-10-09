namespace Umbraco.AI.Core.Utilities;

/// <summary>
/// A current value held per async flow, with nesting: pushing a value makes it current for the code that pushed
/// it and everything it awaits or starts, and disposing the returned entry makes the value it replaced current
/// again, for the flow that disposes it.
/// </summary>
/// <remarks>
/// <para>
/// Work running in parallel each sees its own value, never one another flow pushed. Work keeps the value it
/// started with for its whole life, even after the entry's owner disposes it (e.g. a background task that
/// outlives the call that started it). Disposing entries out of order (the outer one first) is a usage error;
/// like <c>Activity.Current</c>, nothing is repaired.
/// </para>
/// <para>
/// A value pushed inside an async method, including an async iterator between its yields, is not current for its
/// caller. Code that pushes inside an iterator re-enters the entry around each step of the stream it runs
/// (<see cref="Enter"/> with <c>EnterEachStep</c>).
/// </para>
/// <para>
/// Hold one instance per kind of value, in a static field.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value.</typeparam>
internal sealed class AIAmbientStack<T>
    where T : class
{
    private readonly AsyncLocal<Entry?> _current = new();

    /// <summary>
    /// The value current in this async flow, if any.
    /// </summary>
    public T? Current => _current.Value?.Value;

    /// <summary>
    /// Makes <paramref name="value"/> current until the returned entry is disposed.
    /// </summary>
    public Entry Push(T value)
    {
        var entry = new Entry(this, value, _current.Value);
        _current.Value = entry;
        return entry;
    }

    /// <summary>
    /// Makes the value of <paramref name="handle"/> current until the returned handle is disposed, for code that
    /// runs where the pushing flow doesn't reach (the steps of a stream enumerated by a caller).
    /// </summary>
    /// <returns><c>null</c> when <paramref name="handle"/> is not an entry of this stack.</returns>
    public IDisposable? Enter(IDisposable? handle)
    {
        if (handle is not Entry entry || !ReferenceEquals(entry.Stack, this))
        {
            return null;
        }

        var previous = _current.Value;
        _current.Value = entry;
        return new Restore(this, previous);
    }

    /// <summary>
    /// A value made current by <see cref="Push"/>, linked to the entry that was current when it was pushed.
    /// </summary>
    public sealed class Entry : IDisposable
    {
        private readonly Entry? _parent;
        private bool _disposed;

        internal Entry(AIAmbientStack<T> stack, T value, Entry? parent)
        {
            Stack = stack;
            Value = value;
            _parent = parent;
        }

        /// <summary>
        /// The value this entry makes current.
        /// </summary>
        public T Value { get; }

        internal AIAmbientStack<T> Stack { get; }

        /// <summary>
        /// Makes the entry that was current when this one was pushed current again, for the flow disposing it
        /// only. Work in other flows that has this entry keeps it.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (ReferenceEquals(Stack._current.Value, this))
            {
                Stack._current.Value = _parent;
            }
        }
    }

    private sealed class Restore(AIAmbientStack<T> stack, Entry? previous) : IDisposable
    {
        public void Dispose() => stack._current.Value = previous;
    }
}
