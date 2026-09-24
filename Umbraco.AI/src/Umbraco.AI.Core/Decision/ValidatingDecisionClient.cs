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
    private const int MinChoiceOptions = 2;
    private const int MaxChoiceOptions = 255;
    private const int MinScoreLevels = 2;
    private const int MaxScoreLevels = 10;

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

        if (string.IsNullOrWhiteSpace(question.Instructions))
        {
            throw new ArgumentException("Instructions must not be empty or whitespace.", nameof(question));
        }

        switch (question)
        {
            case AIChoiceDecisionQuestion choiceQuestion:
                ValidateChoice(choiceQuestion);
                break;
            case AIScoreDecisionQuestion scoreQuestion:
                ValidateScore(scoreQuestion);
                break;
        }
    }

    private static void ValidateChoice(AIChoiceDecisionQuestion question)
    {
        if (question.Options is null || question.Options.Count is < MinChoiceOptions or > MaxChoiceOptions)
        {
            throw new ArgumentException(
                $"Options must contain between {MinChoiceOptions} and {MaxChoiceOptions} entries.",
                nameof(question));
        }

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in question.Options)
        {
            if (option is null)
            {
                throw new ArgumentException("Options must not contain null entries.", nameof(question));
            }

            if (string.IsNullOrWhiteSpace(option.Key))
            {
                throw new ArgumentException("Option keys must not be empty or whitespace.", nameof(question));
            }

            if (!seenKeys.Add(option.Key))
            {
                throw new ArgumentException($"Duplicate option key '{option.Key}'.", nameof(question));
            }
        }
    }

    private static void ValidateScore(AIScoreDecisionQuestion question)
    {
        if (question.Levels is null || question.Levels.Count is < MinScoreLevels or > MaxScoreLevels)
        {
            throw new ArgumentException(
                $"Levels must contain between {MinScoreLevels} and {MaxScoreLevels} entries.",
                nameof(question));
        }

        if (question.Levels.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Levels must not contain empty or whitespace entries.", nameof(question));
        }
    }
}
