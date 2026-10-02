using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Structural validation for an <see cref="AIDecisionQuestion"/>, shared between
/// <see cref="ValidatingDecisionClient"/> (the last line of defence for any C# caller) and the
/// Management API's <c>AskDecisionController</c> (front-line validation that must run before profile
/// resolution and any provider call — see ARCHITECTURE.md's Security section and SPEC.md's guarantees
/// for <c>POST decision/ask</c>). A single set of rules means the two call sites can never drift.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
internal static class DecisionQuestionValidator
{
    private const int MinChoiceOptions = 2;
    private const int MaxChoiceOptions = 255;
    private const int MinScoreLevels = 2;
    private const int MaxScoreLevels = 10;

    /// <summary>
    /// Validates <paramref name="question"/> against its shape's structural rules.
    /// </summary>
    /// <param name="question">The question to validate.</param>
    /// <returns>
    /// A description of the first rule <paramref name="question"/> breaks, or <see langword="null"/>
    /// when it's valid.
    /// </returns>
    public static string? Validate(AIDecisionQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (string.IsNullOrWhiteSpace(question.Instructions))
        {
            return "Instructions must not be empty or whitespace.";
        }

        return question switch
        {
            AIChoiceDecisionQuestion choiceQuestion => ValidateChoice(choiceQuestion),
            AIScoreDecisionQuestion scoreQuestion => ValidateScore(scoreQuestion),
            _ => null
        };
    }

    private static string? ValidateChoice(AIChoiceDecisionQuestion question)
    {
        if (question.Options is null || question.Options.Count is < MinChoiceOptions or > MaxChoiceOptions)
        {
            return $"Options must contain between {MinChoiceOptions} and {MaxChoiceOptions} entries.";
        }

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in question.Options)
        {
            if (option is null)
            {
                return "Options must not contain null entries.";
            }

            if (string.IsNullOrWhiteSpace(option.Key))
            {
                return "Option keys must not be empty or whitespace.";
            }

            if (!seenKeys.Add(option.Key))
            {
                return $"Duplicate option key '{option.Key}'.";
            }
        }

        return null;
    }

    private static string? ValidateScore(AIScoreDecisionQuestion question)
    {
        if (question.Levels is null || question.Levels.Count is < MinScoreLevels or > MaxScoreLevels)
        {
            return $"Levels must contain between {MinScoreLevels} and {MaxScoreLevels} entries.";
        }

        if (question.Levels.Any(string.IsNullOrWhiteSpace))
        {
            return "Levels must not contain empty or whitespace entries.";
        }

        return null;
    }
}
