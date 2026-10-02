#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

/// <summary>
/// DC-3 (AC2). One representative happy-path case, mirroring
/// <c>AITrackingSpeechToTextClientTests</c> — the same shared <see cref="AIOperationTracker"/>
/// backs every capability's tracking client, so this proves Decision plugs into it the same way
/// rather than re-testing every failure/cancellation branch <c>AITrackingSpeechToTextClientTests</c>
/// already covers for the tracker itself.
/// </summary>
public class AITrackingDecisionClientTests
{
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock;
    private readonly Mock<IAIAuditLogService> _auditLogServiceMock;
    private readonly Mock<IAIAuditLogFactory> _auditLogFactoryMock;
    private readonly Mock<IAIUsageRecordingService> _usageRecordingServiceMock;
    private readonly Mock<IAIUsageRecordFactory> _usageRecordFactoryMock;
    private readonly Mock<IOptionsMonitor<AIAuditLogOptions>> _auditLogOptionsMock;
    private readonly Mock<IOptionsMonitor<AIAnalyticsOptions>> _analyticsOptionsMock;
    private readonly AIAuditLog _auditLog;

    public AITrackingDecisionClientTests()
    {
        _contextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        _auditLogServiceMock = new Mock<IAIAuditLogService>();
        _auditLogFactoryMock = new Mock<IAIAuditLogFactory>();
        _usageRecordingServiceMock = new Mock<IAIUsageRecordingService>();
        _usageRecordFactoryMock = new Mock<IAIUsageRecordFactory>();

        _auditLogOptionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });

        _analyticsOptionsMock = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        _analyticsOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, Guid.NewGuid());
        runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "test-decision-profile");
        runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "fake-decision-provider");
        runtimeContext.SetValue(Constants.ContextKeys.ModelId, "test-model-1");
        _contextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        _auditLog = new AIAuditLog { Id = Guid.NewGuid() };
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(_auditLog);
        _auditLogServiceMock
            .Setup(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _auditLogServiceMock
            .Setup(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
    }

    private static AIDecisionRequest OneQuestion(AIDecisionQuestion question) => new() { State = "text", Questions = [question] };

    [Fact]
    public async Task GetResponseAsync_OnSuccess_QueuesCompleteAudit()
    {
        // Arrange
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 } },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIBinaryDecisionQuestion { Id = "q", Instructions = "is this spam?" }));

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.IsAny<AIAuditResponse?>(),
            CancellationToken.None), Times.Once);
    }

    // Snapshot content — the audit entry must actually carry the request's own state, every question's
    // id, and each answer keyed by the question id it answers, not just a per-kind "Kind" discriminator.

    [Fact]
    public async Task GetResponseAsync_RecordsTheRequestStateInPromptData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 } },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIBinaryDecisionQuestion { Id = "q", Instructions = "is this spam?" }));

        // Assert
        GetProperty(captured.Prompt?.Data, "State").ShouldBe("text");
    }

    [Fact]
    public async Task GetResponseAsync_RecordsTheQuestionIdInPromptData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 } },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIBinaryDecisionQuestion { Id = "q", Instructions = "is this spam?" }));

        // Assert
        var questions = (IEnumerable<object>)GetProperty(captured.Prompt?.Data, "Questions")!;
        GetProperty(questions.Single(), "Id").ShouldBe("q");
    }

    [Fact]
    public async Task GetResponseAsync_WithATwoQuestionRequest_RecordsEveryQuestionInPromptData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                ["a"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 },
                ["b"] = new AIBinaryDecisionAnswer { TrueProbability = 0.1 },
            },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(new AIDecisionRequest
        {
            State = "text",
            Questions =
            [
                new AIBinaryDecisionQuestion { Id = "a", Instructions = "One?" },
                new AIBinaryDecisionQuestion { Id = "b", Instructions = "Two?" },
            ],
        });

        // Assert
        var questions = (IEnumerable<object>)GetProperty(captured.Prompt?.Data, "Questions")!;
        questions.Select(q => GetProperty(q, "Id")).ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task GetResponseAsync_RecordsTheAnswerUnderItsQuestionIdInAuditData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 } },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIBinaryDecisionQuestion { Id = "q", Instructions = "is this spam?" }));

        // Assert
        var answers = (System.Collections.IDictionary)GetProperty(captured.Response?.Data, "Answers")!;
        GetProperty(answers["q"], "TrueProbability").ShouldBe(0.9);
    }

    // DC-3 (AC2 continued) — BuildPromptData/BuildAuditData have one switch arm per question/answer
    // kind plus a fallback arm for anything else. These pin each arm via the only path they're
    // reachable from — the real GetResponseAsync -> audit pipeline — since both methods are private.

    [Fact]
    public async Task GetResponseAsync_WithAChoiceQuestion_RecordsChoiceKindInPromptData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                ["q"] = new AIChoiceDecisionAnswer { Choice = "a", Probabilities = new Dictionary<string, double> { ["a"] = 1 } },
            },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIChoiceDecisionQuestion
        {
            Id = "q",
            Instructions = "Pick",
            Options = [new AIDecisionOption("a"), new AIDecisionOption("b")],
        }));

        // Assert
        var questions = (IEnumerable<object>)GetProperty(captured.Prompt?.Data, "Questions")!;
        GetProperty(questions.Single(), "Kind").ShouldBe("choice");
    }

    [Fact]
    public async Task GetResponseAsync_WithAChoiceQuestion_RecordsChoiceKindInAuditData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                ["q"] = new AIChoiceDecisionAnswer { Choice = "a", Probabilities = new Dictionary<string, double> { ["a"] = 1 } },
            },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIChoiceDecisionQuestion
        {
            Id = "q",
            Instructions = "Pick",
            Options = [new AIDecisionOption("a"), new AIDecisionOption("b")],
        }));

        // Assert
        var answers = (System.Collections.IDictionary)GetProperty(captured.Response?.Data, "Answers")!;
        GetProperty(answers["q"], "Kind").ShouldBe("choice");
    }

    [Fact]
    public async Task GetResponseAsync_WithAScoreQuestion_RecordsScoreKindInPromptData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                ["q"] = new AIScoreDecisionAnswer { Score = 1, Probabilities = new Dictionary<int, double> { [0] = 0.2, [1] = 0.8 } },
            },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIScoreDecisionQuestion
        {
            Id = "q",
            Instructions = "Rate",
            Levels = [new AIDecisionScoreLevel("l0"), new AIDecisionScoreLevel("l1")],
        }));

        // Assert
        var questions = (IEnumerable<object>)GetProperty(captured.Prompt?.Data, "Questions")!;
        GetProperty(questions.Single(), "Kind").ShouldBe("score");
    }

    [Fact]
    public async Task GetResponseAsync_WithAScoreQuestion_RecordsScoreKindInAuditData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer>
            {
                ["q"] = new AIScoreDecisionAnswer { Score = 1, Probabilities = new Dictionary<int, double> { [0] = 0.2, [1] = 0.8 } },
            },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new AIScoreDecisionQuestion
        {
            Id = "q",
            Instructions = "Rate",
            Levels = [new AIDecisionScoreLevel("l0"), new AIDecisionScoreLevel("l1")],
        }));

        // Assert
        var answers = (System.Collections.IDictionary)GetProperty(captured.Response?.Data, "Answers")!;
        GetProperty(answers["q"], "Kind").ShouldBe("score");
    }

    [Fact]
    public async Task GetResponseAsync_WithAnUnrecognisedQuestionSubtype_RecordsItsTypeNameInPromptData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = new UnknownDecisionAnswer() },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new UnknownDecisionQuestion { Id = "q", Instructions = "Something new" }));

        // Assert
        var questions = (IEnumerable<object>)GetProperty(captured.Prompt?.Data, "Questions")!;
        GetProperty(questions.Single(), "Kind").ShouldBe(nameof(UnknownDecisionQuestion));
    }

    [Fact]
    public async Task GetResponseAsync_WithAnUnrecognisedAnswerSubtype_RecordsItsTypeNameInAuditData()
    {
        // Arrange
        var captured = CaptureCompletedAudit();
        var fakeClient = new FakeDecisionClient(_ => new AIDecisionResponse
        {
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = new UnknownDecisionAnswer() },
        });
        var client = new AITrackingDecisionClient(fakeClient, CreateTracker(), _contextAccessorMock.Object);

        // Act
        await client.GetResponseAsync(OneQuestion(new UnknownDecisionQuestion { Id = "q", Instructions = "Something new" }));

        // Assert
        var answers = (System.Collections.IDictionary)GetProperty(captured.Response?.Data, "Answers")!;
        GetProperty(answers["q"], "Kind").ShouldBe(nameof(UnknownDecisionAnswer));
    }

    private AIOperationTracker CreateTracker() => new(
        _contextAccessorMock.Object,
        _auditLogServiceMock.Object,
        _auditLogFactoryMock.Object,
        _auditLogOptionsMock.Object,
        _usageRecordingServiceMock.Object,
        _usageRecordFactoryMock.Object,
        _analyticsOptionsMock.Object,
        NullLogger<AIOperationTracker>.Instance);

    /// <summary>
    /// Wires <see cref="IAIAuditLogService.QueueCompleteAuditLogAsync"/> to record the prompt/response
    /// passed to it, so a test can inspect what <c>AITrackingDecisionClient</c> built for them after
    /// awaiting <c>GetResponseAsync</c>.
    /// </summary>
    private CapturedAudit CaptureCompletedAudit()
    {
        var captured = new CapturedAudit();

        _auditLogServiceMock
            .Setup(x => x.QueueCompleteAuditLogAsync(
                It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()))
            .Callback<AIAuditLog, AIAuditPrompt?, AIAuditResponse?, CancellationToken>((_, p, r, _) =>
            {
                captured.Prompt = p;
                captured.Response = r;
            })
            .Returns(ValueTask.CompletedTask);

        return captured;
    }

    /// <summary>Reads a public property off an anonymous <c>BuildPromptData</c>/<c>BuildAuditData</c> object.</summary>
    private static object? GetProperty(object? data, string propertyName) => data?.GetType().GetProperty(propertyName)?.GetValue(data);

    private sealed class CapturedAudit
    {
        public AIAuditPrompt? Prompt { get; set; }

        public AIAuditResponse? Response { get; set; }
    }

    private sealed class UnknownDecisionAnswer : AIDecisionAnswer
    {
    }

    private sealed class UnknownDecisionQuestion : AIDecisionQuestion<UnknownDecisionAnswer>
    {
    }
}
