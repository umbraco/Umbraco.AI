#pragma warning disable UMBRACOAI_DECISION // Implements the experimental decision capability surface

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Checks that a provider's <see cref="AIDecisionResponse"/> actually answers what an
/// <see cref="AIDecisionRequest"/> asked, per ARCHITECTURE.md's "Checks" (provider output): one answer
/// per question id, no extras, each of its question's kind; every probability in range; a choice or
/// score distribution that covers exactly the options/levels offered and sums close enough to 1; and,
/// when present, a confidence in range.
/// </summary>
/// <remarks>
/// Used only by <see cref="AIErrorClassifyingDecisionClient"/>, which calls it inside the tracking
/// middleware so a mismatch is recorded as a tracked/audited failure rather than a false success — see
/// its own remarks and the <c>provider-contract-checks-inside-tracking</c> memory entry. Split out as
/// its own static class, rather than private methods on the client, purely because the rule set is
/// large enough to read better on its own.
/// </remarks>
/// <remarks>
/// The option/level-specific checks (a choice distribution matching the question's exact option keys,
/// a score distribution matching 0..N-1, a score within [0, N-1]) only run when the question is the
/// concrete built-in <see cref="AIChoiceDecisionQuestion"/>/<see cref="AIScoreDecisionQuestion"/> — not
/// merely something matching the answer's <see cref="AIDecisionQuestion{TAnswer}"/> base, which a
/// third-party subclass could also be. Casting to the concrete type would throw
/// <see cref="InvalidCastException"/> for such a subclass, outside <see cref="AIErrorClassifyingDecisionClient"/>'s
/// own classified-exception handling. The probability-range, sum and confidence checks have no such
/// dependency and still run for every answer regardless of the question's concrete type.
/// </remarks>
internal static class DecisionAnswerChecker
{
    /// <summary>The minimum tolerance a choice/score distribution's sum is allowed to miss 1 by.</summary>
    private const double MinimumSumTolerance = 0.02;

    /// <summary>The extra tolerance added per distribution entry, on top of <see cref="MinimumSumTolerance"/>.</summary>
    private const double PerEntrySumTolerance = 0.005;

    /// <summary>
    /// Checks every question in <paramref name="request"/> was answered, by an answer of its own kind
    /// that holds together, and that <paramref name="response"/> carries no answer for a question that
    /// wasn't asked. Every question's <see cref="AIDecisionQuestion.Id"/> is assumed already validated
    /// non-blank and unique by <see cref="ValidatingDecisionClient"/>, outside this check.
    /// </summary>
    public static void Check(AIDecisionRequest request, AIDecisionResponse response)
    {
        CheckNoUnaskedAnswers(request, response);

        foreach (var question in request.Questions)
        {
            var answer = GetAnswerOrThrow(question, response);
            CheckKind(question, answer);
            CheckAnswer(question, answer);
        }
    }

    private static void CheckNoUnaskedAnswers(AIDecisionRequest request, AIDecisionResponse response)
    {
        var askedIds = request.Questions.Select(q => q.Id!).ToHashSet();

        foreach (var answerId in response.Answers.Keys)
        {
            if (!askedIds.Contains(answerId))
            {
                throw AIDecisionExceptionFactory.CreateExtraAnswerException(answerId);
            }
        }
    }

    private static AIDecisionAnswer GetAnswerOrThrow(AIDecisionQuestion question, AIDecisionResponse response)
    {
        if (!response.Answers.TryGetValue(question.Id!, out var answer))
        {
            throw AIDecisionExceptionFactory.CreateMissingAnswerException(question);
        }

        return answer;
    }

    private static void CheckKind(AIDecisionQuestion question, AIDecisionAnswer answer)
    {
        if (!question.IsExpectedAnswer(answer))
        {
            throw AIDecisionExceptionFactory.CreateAnswerTypeMismatchException(question, answer);
        }
    }

    private static void CheckAnswer(AIDecisionQuestion question, AIDecisionAnswer answer)
    {
        switch (answer)
        {
            case AIBinaryDecisionAnswer binary:
                CheckProbability(question, binary.TrueProbability, "true-probability");
                break;
            case AIChoiceDecisionAnswer choice:
                CheckChoice(question, choice);
                break;
            case AIScoreDecisionAnswer score:
                CheckScore(question, score);
                break;
        }
    }

    /// <summary>
    /// Checks a choice answer. The option-specific checks (distribution keys match the question's
    /// options exactly, <see cref="AIChoiceDecisionAnswer.Choice"/> is one of them) only run when
    /// <paramref name="question"/> is the concrete <see cref="AIChoiceDecisionQuestion"/> — see this
    /// class's remarks.
    /// </summary>
    private static void CheckChoice(AIDecisionQuestion question, AIChoiceDecisionAnswer answer)
    {
        // Choice is `required`, but that's a compile-time guarantee only — a provider adapter
        // deserializing from JSON can still produce a null here (JSON deserialization doesn't enforce
        // `required`), so it's checked like any other provider-controlled value rather than trusted.
        if (answer.Choice is null)
        {
            throw AIDecisionExceptionFactory.CreateInvalidAnswerException(question, "a missing choice");
        }

        if (question is AIChoiceDecisionQuestion choiceQuestion)
        {
            var optionKeys = choiceQuestion.Options.Select(o => o.Key).ToHashSet(StringComparer.Ordinal);

            CheckDistributionKeys(question, optionKeys, answer.Probabilities.Keys.ToHashSet(StringComparer.Ordinal));

            if (!optionKeys.Contains(answer.Choice))
            {
                throw AIDecisionExceptionFactory.CreateInvalidAnswerException(
                    question, $"an unrecognized choice '{AIDecisionExceptionFactory.Truncate(answer.Choice)}'");
            }
        }

        foreach (var probability in answer.Probabilities.Values)
        {
            CheckProbability(question, probability, "probability");
        }

        CheckSum(question, answer.Probabilities.Values);
        CheckConfidence(question, answer.Confidence);
    }

    /// <summary>
    /// Checks a score answer. The level-specific checks (distribution keys match 0..N-1,
    /// <see cref="AIScoreDecisionAnswer.Score"/> within [0, N-1]) only run when <paramref name="question"/>
    /// is the concrete <see cref="AIScoreDecisionQuestion"/> — see this class's remarks.
    /// </summary>
    private static void CheckScore(AIDecisionQuestion question, AIScoreDecisionAnswer answer)
    {
        if (question is AIScoreDecisionQuestion scoreQuestion)
        {
            var levelCount = scoreQuestion.Levels.Count;
            var expectedKeys = Enumerable.Range(0, levelCount).ToHashSet();

            CheckDistributionKeys(question, expectedKeys, answer.Probabilities.Keys.ToHashSet());

            if (!(answer.Score >= 0 && answer.Score <= levelCount - 1))
            {
                throw AIDecisionExceptionFactory.CreateInvalidAnswerException(
                    question, $"a score of {answer.Score} outside [0, {levelCount - 1}]");
            }
        }

        foreach (var probability in answer.Probabilities.Values)
        {
            CheckProbability(question, probability, "probability");
        }

        CheckSum(question, answer.Probabilities.Values);
        CheckConfidence(question, answer.Confidence);
    }

    /// <summary>
    /// Checks <paramref name="probability"/> is in [0, 1]. Written as a positive range test — not
    /// <c>probability &lt; 0 || probability &gt; 1</c> — because every comparison against
    /// <see cref="double.NaN"/> evaluates to <see langword="false"/>, which would let a NaN
    /// probability (a provider bug like any other) silently pass a negated check.
    /// </summary>
    private static void CheckProbability(AIDecisionQuestion question, double probability, string label)
    {
        if (!(probability >= 0 && probability <= 1))
        {
            throw AIDecisionExceptionFactory.CreateInvalidAnswerException(
                question, $"a {label} of {probability} outside [0, 1]");
        }
    }

    private static void CheckDistributionKeys<TKey>(AIDecisionQuestion question, HashSet<TKey> expected, HashSet<TKey> actual)
        where TKey : notnull
    {
        if (!actual.SetEquals(expected))
        {
            throw AIDecisionExceptionFactory.CreateInvalidAnswerException(
                question, "a probability distribution that doesn't exactly match the question's options");
        }
    }

    /// <summary>
    /// Checks <paramref name="probabilities"/> sums to 1 within <c>max(0.02, 0.005 × count)</c>. Written
    /// as a positive range test for the same NaN reason as <see cref="CheckProbability"/> — a NaN sum
    /// (e.g. from a NaN probability slipping past an earlier check) must still fail here rather than
    /// have the negated comparison silently pass.
    /// </summary>
    private static void CheckSum(AIDecisionQuestion question, IEnumerable<double> probabilities)
    {
        var values = probabilities as ICollection<double> ?? probabilities.ToList();
        var sum = values.Sum();
        var tolerance = Math.Max(MinimumSumTolerance, PerEntrySumTolerance * values.Count);

        if (!(Math.Abs(sum - 1.0) <= tolerance))
        {
            throw AIDecisionExceptionFactory.CreateInvalidAnswerException(
                question, $"probabilities summing to {sum} instead of 1");
        }
    }

    private static void CheckConfidence(AIDecisionQuestion question, double? confidence)
    {
        if (confidence is { } value && !(value >= 0 && value <= 1))
        {
            throw AIDecisionExceptionFactory.CreateInvalidAnswerException(
                question, $"a confidence of {value} outside [0, 1]");
        }
    }
}
