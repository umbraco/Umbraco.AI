using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A decision client decorator that validates an <see cref="AIDecisionRequest"/> before it is
/// allowed to reach any inner <see cref="IAIDecisionClient"/>.
/// </summary>
/// <remarks>
/// Applied in front of every provider's <see cref="IAIDecisionClient"/>, mirroring how
/// <c>AIErrorClassifyingSpeechToTextClient</c> wraps every <c>ISpeechToTextClient</c> today — a
/// caller error (an empty batch, duplicate/blank question ids, blank instructions, an
/// <see cref="AIChoiceDecisionQuestion"/> with too few/too many options, an
/// <see cref="AIScoreDecisionQuestion"/> with too few/too many levels) is rejected here, before any
/// provider SDK call is made.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
internal sealed class ValidatingDecisionClient : IAIDecisionClient
{
    private readonly IAIDecisionClient _innerClient;

    public ValidatingDecisionClient(IAIDecisionClient innerClient)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    /// <inheritdoc />
    public Task<AIDecisionResponse> GetResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
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

    private static void Validate(AIDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var error = DecisionQuestionValidator.ValidateRequest(request);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(request));
        }
    }
}
