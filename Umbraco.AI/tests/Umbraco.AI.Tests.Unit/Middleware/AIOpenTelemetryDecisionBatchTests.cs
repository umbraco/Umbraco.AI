#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

// DR-14 — Ask several questions in one call from C# (AC4): batch telemetry tags.
//
// ASSUMPTIONS (T29 builder confirms/adjusts): FakeDecisionClient takes
// Func<AIDecisionRequest, AIDecisionResponse>; the middleware's client exposes GetResponseAsync.
// Replaces the per-kind "gen_ai.request.kind" tests in AIOpenTelemetryDecisionMiddlewareTests.

using System.Diagnostics;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIOpenTelemetryDecisionBatchTests
{
    /// <summary>Filter the process-wide listener to this middleware's span (see otel-test-shared-activity-source).</summary>
    private const string DecisionSpanName = "gen_ai.decision";

    private static AIDecisionRequest BinaryAndScore() => new()
    {
        State = "text",
        Questions =
        [
            new AIBinaryDecisionQuestion { Id = "spam", Instructions = "Is this spam?" },
            new AIScoreDecisionQuestion
            {
                Id = "quality",
                Instructions = "How good?",
                Levels = [new AIDecisionScoreLevel("poor"), new AIDecisionScoreLevel("good")],
            },
        ],
    };

    private static FakeDecisionClient AnsweringBoth() => new(_ => new AIDecisionResponse
    {
        Answers = new Dictionary<string, AIDecisionAnswer>
        {
            ["spam"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 },
            ["quality"] = new AIScoreDecisionAnswer
            {
                Score = 1,
                Probabilities = new Dictionary<int, double> { [0] = 0.0, [1] = 1.0 },
            },
        },
    });

    private static async Task<Activity?> CaptureActivityAsync(FakeDecisionClient innerClient, AIDecisionRequest request)
    {
        Activity? captured = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AITelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == DecisionSpanName)
                {
                    captured = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var client = new AIOpenTelemetryDecisionMiddleware().Apply(innerClient);
        await client.GetResponseAsync(request);

        return captured;
    }

    #region Happy path

    public class GivenABinaryAndAScoreQuestion
    {
        private readonly Activity? _activity = CaptureActivityAsync(AnsweringBoth(), BinaryAndScore()).GetAwaiter().GetResult();

        [Fact]
        public void TagsTheQuestionCount() => _activity!.GetTagItem("gen_ai.decision.question_count").ShouldBe(2);

        [Fact]
        public void TagsTheDistinctKinds() => _activity!.GetTagItem("gen_ai.request.kind").ShouldBe("binary,score");

        [Fact]
        public void NoLongerTagsAResponseConfidence() => _activity!.GetTagItem("gen_ai.response.confidence").ShouldBeNull();
    }

    public class GivenTwoBinaryQuestionsAndAScoreQuestion
    {
        private static AIDecisionRequest Request() => new()
        {
            State = "text",
            Questions =
            [
                new AIBinaryDecisionQuestion { Id = "spam", Instructions = "Is this spam?" },
                new AIBinaryDecisionQuestion { Id = "urgent", Instructions = "Is this urgent?" },
                new AIScoreDecisionQuestion
                {
                    Id = "quality",
                    Instructions = "How good?",
                    Levels = [new AIDecisionScoreLevel("poor"), new AIDecisionScoreLevel("good")],
                },
            ],
        };

        private static FakeDecisionClient AnsweringAll() => new(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                ["spam"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 },
                ["urgent"] = new AIBinaryDecisionAnswer { TrueProbability = 0.1 },
                ["quality"] = new AIScoreDecisionAnswer
                {
                    Score = 1,
                    Probabilities = new Dictionary<int, double> { [0] = 0.0, [1] = 1.0 },
                },
            },
        });

        private readonly Activity? _activity = CaptureActivityAsync(AnsweringAll(), Request()).GetAwaiter().GetResult();

        [Fact]
        public void TagsTheRepeatedKindOnlyOnce() => _activity!.GetTagItem("gen_ai.request.kind").ShouldBe("binary,score");
    }

    #endregion
}
