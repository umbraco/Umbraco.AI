using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// An extraction client decorator that validates an <see cref="AIExtractionRequest"/> before it is
/// allowed to reach any inner <see cref="IAIExtractionClient"/>.
/// </summary>
/// <remarks>
/// Applied outermost in front of every provider's <see cref="IAIExtractionClient"/>, mirroring
/// <see cref="Decision.IAIDecisionClient"/>'s validating wrapper — a caller error (an empty batch,
/// blank, duplicate or malformed field ids, blank instructions or state, out-of-range decimals) is
/// rejected here, before any provider call is made, and is never misreported as a provider failure.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
internal sealed class ValidatingExtractionClient : IAIExtractionClient
{
    private readonly IAIExtractionClient _innerClient;

    public ValidatingExtractionClient(IAIExtractionClient innerClient)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    /// <inheritdoc />
    public Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        return _innerClient.GetResponseAsync(request, options, cancellationToken);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => _innerClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();

    private static void Validate(AIExtractionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var error = ExtractionFieldValidator.ValidateRequest(request);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(request));
        }
    }
}
