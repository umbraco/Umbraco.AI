#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-15 — Provider answers are complete and consistent (AC1-AC9).
//
// The checks live in AIErrorClassifyingDecisionClient, inside tracking (ARCHITECTURE "Checks"), so
// every spec goes through the real factory pipeline (DecisionTrackingAndChecksHarness) rather than
// newing the classifier directly. Tolerance under test: |sum - 1| <= max(0.02, 0.005 x count).

using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

public class ProviderAnswerChecksTests
{
    private static AIDecisionRequest One(AIDecisionQuestion question) => new() { State = "text", Questions = [question] };

    private static AIDecisionQuestion Binary() => new AIBinaryDecisionQuestion { Id = "q", Instructions = "Is it?" };

    private static AIDecisionQuestion Choice(params string[] keys) => new AIChoiceDecisionQuestion
    {
        Id = "q",
        Instructions = "Which?",
        Options = keys.Select(k => new AIDecisionOption(k)).ToList(),
    };

    private static AIDecisionQuestion Score(int levels) => new AIScoreDecisionQuestion
    {
        Id = "q",
        Instructions = "How much?",
        Levels = Enumerable.Range(0, levels).Select(i => new AIDecisionScoreLevel($"level {i}")).ToList(),
    };

    private static async Task<Func<Task<AIDecisionResponse>>> ArrangeAsync(AIDecisionQuestion question, AIDecisionAnswer answer)
    {
        var harness = await DecisionTrackingAndChecksHarness.CreateAsync(new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = answer },
        }));
        return () => harness.Client.GetResponseAsync(One(question));
    }

    /// <summary>A probability distribution over 0..<paramref name="levels"/>-1, all mass on index 0.</summary>
    private static Dictionary<int, double> ScoreProbabilities(int levels, double indexZeroMass) =>
        Enumerable.Range(0, levels).ToDictionary(i => i, i => i == 0 ? indexZeroMass : 0.0);

    /// <summary>
    /// A question asking for an <see cref="AIChoiceDecisionAnswer"/> that isn't the concrete, built-in
    /// <see cref="AIChoiceDecisionQuestion"/> — standing in for a third-party subclass of
    /// <see cref="AIDecisionQuestion{TAnswer}"/> directly (see that type's own remarks).
    /// </summary>
    private sealed class CustomChoiceDecisionQuestion : AIDecisionQuestion<AIChoiceDecisionAnswer>;

    #region Happy path

    public class GivenChoiceProbabilitiesRoundedToSumPoint99
    {
        [Fact]
        public async Task Passes()
        {
            var act = await ArrangeAsync(Choice("a", "b", "c"), new AIChoiceDecisionAnswer
            {
                Choice = "a",
                Probabilities = new Dictionary<string, double> { ["a"] = 0.33, ["b"] = 0.33, ["c"] = 0.33 },
            });

            await Should.NotThrowAsync(act);
        }
    }

    public class GivenATenLevelScoreWithinThePerEntryTolerance
    {
        [Fact]
        public async Task Passes()
        {
            var act = await ArrangeAsync(Score(10), new AIScoreDecisionAnswer
            {
                Score = 0,
                Probabilities = ScoreProbabilities(10, indexZeroMass: 0.96),
            });

            await Should.NotThrowAsync(act);
        }
    }

    public class GivenACustomChoiceQuestionSubclass
    {
        [Fact]
        public async Task PassesAValidAnswerWithoutCheckingItsOptions()
        {
            var question = new CustomChoiceDecisionQuestion { Id = "q", Instructions = "Which?" };
            var act = await ArrangeAsync(question, new AIChoiceDecisionAnswer
            {
                Choice = "anything",
                Probabilities = new Dictionary<string, double> { ["anything"] = 1.0 },
            });

            await Should.NotThrowAsync(act);
        }
    }

    #endregion

    #region Sad path

    public class GivenAnInconsistentAnswer
    {
        public static TheoryData<string, AIDecisionQuestion, AIDecisionAnswer> Cases => new()
        {
            { "true-probability above 1", Binary(), new AIBinaryDecisionAnswer { TrueProbability = 1.2 } },
            {
                "choice distribution missing an option", Choice("a", "b", "c"),
                new AIChoiceDecisionAnswer { Choice = "a", Probabilities = new Dictionary<string, double> { ["a"] = 0.6, ["b"] = 0.4 } }
            },
            {
                "choice that wasn't offered", Choice("a", "b"),
                new AIChoiceDecisionAnswer { Choice = "z", Probabilities = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.5 } }
            },
            {
                "choice probabilities summing to 0.8", Choice("a", "b"),
                new AIChoiceDecisionAnswer { Choice = "a", Probabilities = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.3 } }
            },
            {
                "score above the top level", Score(3),
                new AIScoreDecisionAnswer { Score = 2.5, Probabilities = new Dictionary<int, double> { [0] = 0.0, [1] = 0.0, [2] = 1.0 } }
            },
            {
                "score distribution with the wrong keys", Score(3),
                new AIScoreDecisionAnswer { Score = 1.0, Probabilities = new Dictionary<int, double> { [0] = 0.2, [1] = 0.6, [3] = 0.2 } }
            },
            {
                "confidence below 0", Choice("a", "b"),
                new AIChoiceDecisionAnswer { Choice = "a", Confidence = -0.1, Probabilities = new Dictionary<string, double> { ["a"] = 0.6, ["b"] = 0.4 } }
            },
            { "true-probability is NaN", Binary(), new AIBinaryDecisionAnswer { TrueProbability = double.NaN } },
            {
                "a choice probability is NaN", Choice("a", "b"),
                new AIChoiceDecisionAnswer { Choice = "a", Probabilities = new Dictionary<string, double> { ["a"] = double.NaN, ["b"] = 0.5 } }
            },
            {
                "score is NaN", Score(3),
                new AIScoreDecisionAnswer { Score = double.NaN, Probabilities = new Dictionary<int, double> { [0] = 0.0, [1] = 0.0, [2] = 1.0 } }
            },
            {
                "confidence is NaN", Choice("a", "b"),
                new AIChoiceDecisionAnswer { Choice = "a", Confidence = double.NaN, Probabilities = new Dictionary<string, double> { ["a"] = 0.6, ["b"] = 0.4 } }
            },
            {
                "ten-level score summing to 0.94, outside the per-entry tolerance", Score(10),
                new AIScoreDecisionAnswer { Score = 0, Probabilities = ScoreProbabilities(10, indexZeroMass: 0.94) }
            },
            {
                "choice is null", Choice("a", "b"),
                new AIChoiceDecisionAnswer { Choice = null!, Probabilities = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.5 } }
            },
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task ThrowsAIProviderException(string _, AIDecisionQuestion question, AIDecisionAnswer answer)
        {
            var act = await ArrangeAsync(question, answer);

            await Should.ThrowAsync<AIProviderException>(act);
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task RecordsAFailedCall(string _, AIDecisionQuestion question, AIDecisionAnswer answer)
        {
            var harness = await DecisionTrackingAndChecksHarness.CreateAsync(new FakeDecisionClient(_ => new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = answer },
            }));

            await Should.ThrowAsync<AIProviderException>(() => harness.Client.GetResponseAsync(One(question)));

            harness.AuditLogServiceMock.Verify(
                x => x.QueueRecordAuditLogFailureAsync(
                    It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    /// <summary>DR-14 AC9 — a provider that skips a question in a multi-question batch is recorded as
    /// a failed call, the same as a single-question inconsistent answer.</summary>
    public class GivenATwoQuestionBatchWhereTheProviderSkipsOne
    {
        [Fact]
        public async Task RecordsAFailedCall()
        {
            var request = new AIDecisionRequest
            {
                State = "text",
                Questions =
                [
                    new AIBinaryDecisionQuestion { Id = "a", Instructions = "Is it?" },
                    new AIBinaryDecisionQuestion { Id = "b", Instructions = "Is it also?" },
                ],
            };

            var harness = await DecisionTrackingAndChecksHarness.CreateAsync(new FakeDecisionClient(_ => new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer> { ["a"] = new AIBinaryDecisionAnswer { TrueProbability = 0.5 } },
            }));

            await Should.ThrowAsync<AIProviderException>(() => harness.Client.GetResponseAsync(request));

            harness.AuditLogServiceMock.Verify(
                x => x.QueueRecordAuditLogFailureAsync(
                    It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    #endregion
}
