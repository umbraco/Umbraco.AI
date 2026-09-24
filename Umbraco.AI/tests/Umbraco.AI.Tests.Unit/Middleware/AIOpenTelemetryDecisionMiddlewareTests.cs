#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Diagnostics;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

// DC-3 — the "gen_ai.request.kind" tag has one branch per question kind plus a fallback for
// anything else; each is only reachable through the real AskAsync -> Activity pipeline, since
// QuestionKind is private.
public class AIOpenTelemetryDecisionMiddlewareTests
{
    [Fact]
    public void Apply_ReturnsWrappedClient()
    {
        // Arrange
        var innerClient = new FakeDecisionClient();
        var middleware = new AIOpenTelemetryDecisionMiddleware();

        // Act
        var result = middleware.Apply(innerClient);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(innerClient);
    }

    [Fact]
    public async Task AskAsync_WithABinaryQuestion_TagsRequestKindAsBinary()
    {
        var activity = await CaptureActivityAsync(
            new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.9 }),
            new AIBinaryDecisionQuestion { Instructions = "Is this spam?" });

        GetTag(activity, "gen_ai.request.kind").ShouldBe("binary");
    }

    [Fact]
    public async Task AskAsync_WithAChoiceQuestion_TagsRequestKindAsChoice()
    {
        var activity = await CaptureActivityAsync(
            new FakeDecisionClient(_ => new AIChoiceDecisionResponse { Choice = "a", ChoiceConfidence = 0.9 }),
            new AIChoiceDecisionQuestion
            {
                Instructions = "Pick",
                Options = [new AIDecisionOption("a"), new AIDecisionOption("b")],
            });

        GetTag(activity, "gen_ai.request.kind").ShouldBe("choice");
    }

    [Fact]
    public async Task AskAsync_WithAScoreQuestion_TagsRequestKindAsScore()
    {
        var activity = await CaptureActivityAsync(
            new FakeDecisionClient(_ => new AIScoreDecisionResponse { Score = 1, Level = "l1", ScoreConfidence = 0.9 }),
            new AIScoreDecisionQuestion { Instructions = "Rate", Levels = ["l0", "l1"] });

        GetTag(activity, "gen_ai.request.kind").ShouldBe("score");
    }

    [Fact]
    public async Task AskAsync_WithAnUnrecognisedQuestionSubtype_TagsRequestKindWithItsTypeName()
    {
        var activity = await CaptureActivityAsync(
            new FakeDecisionClient(_ => new UnknownDecisionResponse()),
            new UnknownDecisionQuestion { Instructions = "Something new" });

        GetTag(activity, "gen_ai.request.kind").ShouldBe(nameof(UnknownDecisionQuestion));
    }

    /// <summary>
    /// The span name <c>AIOpenTelemetryDecisionMiddleware</c> passes to
    /// <see cref="ActivitySource.StartActivity(string)"/> — used to filter the listener below so it
    /// only ever captures a decision span, never one from another middleware sharing the same
    /// <see cref="AITelemetry.SourceName"/> (chat, embedding, image generation, speech-to-text all
    /// use that same source name, and <see cref="ActivitySource.AddActivityListener"/> registration
    /// is process-wide, so those middlewares' spans would otherwise be visible here too if their
    /// tests happen to run concurrently).
    /// </summary>
    private const string DecisionSpanName = "gen_ai.decision";

    /// <summary>
    /// Applies the middleware, asks <paramref name="question"/> through it while an
    /// <see cref="ActivityListener"/> is subscribed to <see cref="AITelemetry.SourceName"/>, and
    /// returns the resulting <see cref="Activity"/> — the only way to observe a tag set by the
    /// private <c>AIOpenTelemetryDecisionClient</c> nested inside the middleware.
    /// </summary>
    private static async Task<Activity?> CaptureActivityAsync(FakeDecisionClient innerClient, AIDecisionQuestion question)
    {
        Activity? captured = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AITelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                // Only capture this call's own decision span — other AITelemetry.SourceName
                // middleware (chat/embedding/image-generation/speech-to-text) could otherwise
                // report a stopped activity here too, since the listener is registered globally.
                if (activity.OperationName == DecisionSpanName)
                {
                    captured = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var client = new AIOpenTelemetryDecisionMiddleware().Apply(innerClient);
        await client.AskAsync(question);

        return captured;
    }

    private static object? GetTag(Activity? activity, string tagName) => activity?.GetTagItem(tagName);

    private sealed class UnknownDecisionResponse : AIDecisionResponse
    {
        public override double Confidence => 0.5;
    }

    private sealed class UnknownDecisionQuestion : AIDecisionQuestion<UnknownDecisionResponse>
    {
    }
}
