using System.Runtime.CompilerServices;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Runs a stream's steps in a runtime context scope.
/// </summary>
internal static class AIRuntimeContextStreamExtensions
{
    /// <summary>
    /// Makes <paramref name="scope"/>, or with no scope the scope current now, current around each step of
    /// <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    /// The current context is held per async flow, and an async iterator's steps run in its caller's flow, so a
    /// scope made current inside an iterator is no longer current once it has yielded. Streaming entry points run
    /// the stream they wrap through this: with the scope they created, or, when they run in their caller's
    /// context (<paramref name="scope"/> is <c>null</c>), with the scope current when this is called. That covers
    /// a caller whose own iterator created the scope. With no scope at all, the stream is returned as is.
    /// </remarks>
    public static IAsyncEnumerable<T> WithRuntimeContext<T>(this IAsyncEnumerable<T> source, IAIRuntimeContextScope? scope)
        => (scope ?? AIRuntimeContextScopeProvider.CurrentScope) is { } entered ? Enumerate(source, entered) : source;

    private static async IAsyncEnumerable<T> Enumerate<T>(
        IAsyncEnumerable<T> source,
        IAIRuntimeContextScope scope,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<T> enumerator;
        using (AIRuntimeContextScopeProvider.Enter(scope))
        {
            enumerator = source.GetAsyncEnumerator(cancellationToken);
        }

        try
        {
            while (true)
            {
                using (AIRuntimeContextScopeProvider.Enter(scope))
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        yield break;
                    }
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            using (AIRuntimeContextScopeProvider.Enter(scope))
            {
                await enumerator.DisposeAsync();
            }
        }
    }
}
