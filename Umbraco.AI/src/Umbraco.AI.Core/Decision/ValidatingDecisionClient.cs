using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A decision client decorator that validates an <see cref="AIDecisionQuestion"/> before it is
/// allowed to reach any inner <see cref="IAIDecisionClient"/>.
/// </summary>
/// <remarks>
/// Applied in front of every provider's <see cref="IAIDecisionClient"/>, mirroring how
/// <c>AIErrorClassifyingSpeechToTextClient</c> wraps every <c>ISpeechToTextClient</c> today — a
/// caller error (blank instructions, an <see cref="AIChoiceDecisionQuestion"/> with too few/too many
/// options, an <see cref="AIScoreDecisionQuestion"/> with too few/too many levels) is rejected here,
/// before any provider SDK call is made.
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
    public Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Validate(question);

        return _innerClient.AskAsync(question, options, cancellationToken);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => _innerClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();

    private static void Validate(AIDecisionQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        var error = DecisionQuestionValidator.Validate(question);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(question));
        }
    }
}
