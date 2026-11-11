using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Extensions;

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
/// <para>
/// A provider can also report a failure inside its response as <see cref="ErrorContent"/> instead of
/// throwing. When that error always ends the call, such as an account with no credit left, it is
/// thrown here as a classified <see cref="AIProviderException"/>, the same as when the provider
/// rejects the request outright. Any other <see cref="ErrorContent"/> passes through unchanged.
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
        ChatResponse response;
        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken);
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

        if (response.GetTerminalProviderError() is { } error && ClassifyResponseError(error) is { } classified)
        {
            throw classified;
        }

        return response;
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

                // Any update, not only the last: an out-of-credit error always ends the call, and the
                // stream's end isn't known yet.
                foreach (var content in update.Contents)
                {
                    if (content is ErrorContent error && ClassifyResponseError(error) is { } classified)
                    {
                        throw classified;
                    }
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

    private static AIProviderException? ClassifyResponseError(ErrorContent error)
        => ProviderErrorMapping.FromResponseError(error.ErrorCode, error.Message) is { } info
            ? new AIProviderException(info)
            : null;
}
