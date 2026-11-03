using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// An extraction client decorator that translates provider SDK exceptions into a classified
/// <see cref="AIProviderException"/> using the originating provider's
/// <see cref="IAIProvider.ClassifyError"/>, and rejects a provider's response when it doesn't answer
/// what was asked.
/// </summary>
/// <remarks>
/// Applied innermost by <see cref="AIExtractionClientFactory"/> — around the provider's client
/// and beneath the middleware pipeline, mirroring <c>AIErrorClassifyingSpeechToTextClient</c>.
/// Cancellation propagates untouched; an already-classified <see cref="AIProviderException"/> passes
/// through unchanged.
/// </remarks>
/// <remarks>
/// Unlike <see cref="ValidatingExtractionClient"/>, this class does not sit in front of every provider
/// client — <see cref="AIExtractionClientFactory"/> wraps this innermost and <see cref="ValidatingExtractionClient"/>
/// outermost, so a caller error (an invalid <see cref="AIExtractionRequest"/>) never reaches here to be
/// misreported as a provider failure.
/// </remarks>
/// <remarks>
/// Also rejects a provider's answer that doesn't actually answer what was asked — see
/// <see cref="ExtractionAnswerChecker"/> for the full rule set (missing/extra answers, the wrong answer
/// shape, an out-of-range confidence or null probability) — as a classified
/// <see cref="AIProviderException"/>: a provider bug, not a caller error. This has to happen here rather
/// than up in <see cref="AIExtractionService"/>: <see cref="AIExtractionClientFactory"/> wraps this class
/// *inside* the tracking middleware, so throwing from here (instead of after the whole pipeline returns)
/// means <see cref="Observability.IAIOperationTracker"/> sees the failure and records it as such, rather
/// than recording success and only then having the caller told otherwise.
/// </remarks>
internal sealed class AIErrorClassifyingExtractionClient : IAIExtractionClient
{
    private readonly IAIExtractionClient _innerClient;
    private readonly IAIProvider _provider;

    public AIErrorClassifyingExtractionClient(IAIExtractionClient innerClient, IAIProvider provider)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public async Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        AIExtractionResponse response;
        try
        {
            response = await _innerClient.GetResponseAsync(request, options, cancellationToken);
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

        ExtractionAnswerChecker.Check(request, response);

        return response;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == GetType())
        {
            return this;
        }

        return _innerClient.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();

    private AIProviderException Classify(Exception ex) => new(_provider.ClassifyError(ex), ex);
}
