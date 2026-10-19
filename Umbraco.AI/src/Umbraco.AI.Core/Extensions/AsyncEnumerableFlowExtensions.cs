using System.Runtime.CompilerServices;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Runs a stream's steps inside state held per async flow.
/// </summary>
internal static class AsyncEnumerableFlowExtensions
{
    /// <summary>
    /// Calls <paramref name="enter"/> around each step of <paramref name="source"/> (getting the enumerator, each
    /// move and the disposal), disposing what it returns after the step.
    /// </summary>
    /// <remarks>
    /// <see cref="AsyncLocal{T}"/> state made current inside an async iterator is no longer current once it has
    /// yielded, because its later steps run in its consumer's flow. An iterator that makes such state current
    /// runs the stream it wraps through this, so every step of that stream sees the state.
    /// </remarks>
    public static async IAsyncEnumerable<T> EnterEachStep<T>(
        this IAsyncEnumerable<T> source,
        Func<IDisposable?> enter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<T> enumerator;
        using (enter())
        {
            enumerator = source.GetAsyncEnumerator(cancellationToken);
        }

        try
        {
            while (true)
            {
                using (enter())
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
            using (enter())
            {
                await enumerator.DisposeAsync();
            }
        }
    }
}
