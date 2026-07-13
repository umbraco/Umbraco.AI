using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// A chat client decorator that translates provider SDK exceptions into a classified
/// <see cref="AIProviderException"/> using the originating provider's
/// <see cref="IAIProvider.ClassifyError"/>.
/// </summary>
/// <remarks>
/// <para>
/// Applied innermost by <see cref="AIChatClientFactory"/> — directly around the provider's client
/// and beneath the middleware pipeline — so it only ever sees transport/SDK exceptions, and every
/// model round-trip made inside function-invoking middleware passes back through it.
/// </para>
/// <para>
/// Cancellation propagates untouched only when the caller's own <see cref="CancellationToken"/>
/// was actually signalled; an <see cref="OperationCanceledException"/> raised for any other reason
/// (most commonly a client-side HTTP timeout, which the underlying SDK reports as a
/// <see cref="TaskCanceledException"/>) is still classified so it gets a meaningful message instead
/// of leaking the runtime's raw cancellation text. An already-classified
/// <see cref="AIProviderException"/> passes through unchanged so it is never double-wrapped.
/// </para>
/// </remarks>
internal sealed class AIErrorClassifyingChatClient : DelegatingChatClient
{
    private readonly IAIProvider _provider;

    public AIErrorClassifyingChatClient(IChatClient innerClient, IAIProvider provider)
        : base(innerClient)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AIProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Classify(ex);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Manual enumerator pattern: 'yield return' cannot live inside a try/catch, so we catch
        // around MoveNextAsync and rethrow a classified exception out of the iterator.
        var enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = enumerator.Current;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (AIProviderException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw Classify(ex);
                }

                yield return update;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private AIProviderException Classify(Exception ex)
        => new(_provider.ClassifyError(ex), ex);
}
