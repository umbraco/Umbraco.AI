using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// A speech-to-text client decorator that translates provider SDK exceptions into a classified
/// <see cref="AIProviderException"/> using the originating provider's
/// <see cref="IAIProvider.ClassifyError"/>.
/// </summary>
/// <remarks>
/// Applied innermost by <see cref="AISpeechToTextClientFactory"/> — around the provider's client
/// and beneath the middleware pipeline. Cancellation propagates untouched; an already-classified
/// <see cref="AIProviderException"/> passes through unchanged.
/// </remarks>
internal sealed class AIErrorClassifyingSpeechToTextClient : AIBoundSpeechToTextClientBase
{
    private readonly IAIProvider _provider;

    public AIErrorClassifyingSpeechToTextClient(ISpeechToTextClient innerClient, IAIProvider provider)
        : base(innerClient)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetTextAsync(audioSpeechStream, options, cancellationToken);
        }
        catch (OperationCanceledException)
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
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Manual enumerator pattern: 'yield return' cannot live inside a try/catch, so we catch
        // around MoveNextAsync and rethrow a classified exception out of the iterator.
        var enumerator = base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                SpeechToTextResponseUpdate update;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = enumerator.Current;
                }
                catch (OperationCanceledException)
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
