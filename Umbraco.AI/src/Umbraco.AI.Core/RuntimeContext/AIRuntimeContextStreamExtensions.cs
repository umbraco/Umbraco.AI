using System.Runtime.CompilerServices;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Runs a stream's steps in a runtime context scope.
/// </summary>
internal static class AIRuntimeContextStreamExtensions
{
    /// <summary>
    /// Makes <paramref name="scope"/> current around each step of <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    /// The current context is held per async flow, and an async iterator's steps run in its caller's flow, so a
    /// scope an iterator creates is no longer current once it has yielded. An iterator that creates a scope runs
    /// the stream it wraps through this, so every step of that stream (and its disposal) sees the scope. With a
    /// <c>null</c> scope (the call runs in its caller's context) the stream is returned as is.
    /// </remarks>
    public static IAsyncEnumerable<T> WithRuntimeContext<T>(this IAsyncEnumerable<T> source, IAIRuntimeContextScope? scope)
        => scope is null ? source : Enumerate(source, scope);

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
