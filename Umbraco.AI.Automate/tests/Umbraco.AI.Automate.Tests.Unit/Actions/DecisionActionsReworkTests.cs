// DR-9 — Branch automations on a decision (AC1, AC1b, AC3b, AC8): the reworked single-question actions.
//
// The actions call IAIDecisionService.AskAsync<TAnswer>(Action<AIDecisionBuilder>,
// AIDecisionQuestion<TAnswer>, string? state, CancellationToken) and get AIDecisionResponse<TAnswer>.
// AskYesNoDecisionSettings gains Threshold (double, default 0.5); AskYesNoDecisionOutput loses Confidence.
// The staged assumptions matched the real signatures exactly, so no spec-side fixes were needed.
// Supersedes the yes/no output scenario in DecisionActionsTests that asserts Confidence.
//
// Also covers (added in review follow-up): Context sent as state for Ask pick-one/Ask score,
// Threshold = NaN, Choice/Score Confidence staying null when the provider reports none, and the
// Ask score nearest-level rounding (including the away-from-zero midpoint case) with three levels.
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Xunit;

#pragma warning disable UMBRACOAI_DECISION

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

public class DecisionActionsReworkTests
{
    private readonly Mock<IAIDecisionService> _decisionServiceMock = new();
    private readonly Mock<IAIExperimentalFeatures> _experimentalMock = new();
    private readonly ActionInfrastructure _infrastructure = new(new Mock<IEditableModelResolver>().Object);

    public DecisionActionsReworkTests()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(true);
    }

    #region Scenario: "Ask yes/no" and the provider answers true-probability 0.9

    [Fact]
    public async Task AskYesNo_OutputsAnswerTrueAtTheDefaultThreshold()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?" }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Answer.ShouldBeTrue();
    }

    [Fact]
    public async Task AskYesNo_OutputsTheProbability()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?" }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Probability.ShouldBe(0.9);
    }

    [Fact]
    public void AskYesNoOutput_HasNoConfidence()
        => typeof(AskYesNoDecisionOutput).GetProperty("Confidence").ShouldBeNull();

    #endregion

    #region Scenario: "Ask yes/no" with Threshold 0.95 and the provider answers 0.9

    [Fact]
    public async Task AskYesNo_AboveTheProbability_OutputsAnswerFalse()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(
            YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?", Threshold = 0.95 }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Answer.ShouldBeFalse();
    }

    #endregion

    #region Scenario: Context is sent as the request's state

    [Fact]
    public async Task AskYesNo_SendsContextAsState()
    {
        string? sentState = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIBinaryDecisionAnswer>, string?, CancellationToken>((_, _, state, _) => sentState = state)
            .ReturnsAsync(Binary(0.9));

        await CreateYesNo().ExecuteAsync(
            YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?", Context = "some text" }), CancellationToken.None);

        sentState.ShouldBe("some text");
    }

    [Fact]
    public async Task AskChoice_SendsContextAsState()
    {
        string? sentState = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIChoiceDecisionAnswer>, string?, CancellationToken>((_, _, state, _) => sentState = state)
            .ReturnsAsync(Choice("a", null));

        await CreateChoice().ExecuteAsync(
            ChoiceContext(new AskChoiceDecisionSettings
            {
                Instructions = "Pick",
                Context = "some text",
                Options = [new AskChoiceDecisionOption { Key = "a" }, new AskChoiceDecisionOption { Key = "b" }],
            }), CancellationToken.None);

        sentState.ShouldBe("some text");
    }

    [Fact]
    public async Task AskScore_SendsContextAsState()
    {
        string? sentState = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIScoreDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIScoreDecisionAnswer>, string?, CancellationToken>((_, _, state, _) => sentState = state)
            .ReturnsAsync(Score(0.0, null));

        await CreateScore().ExecuteAsync(
            ScoreContext(new AskScoreDecisionSettings { Instructions = "Rate", Context = "some text", Levels = ["low", "high"] }), CancellationToken.None);

        sentState.ShouldBe("some text");
    }

    #endregion

    #region Sad path: "Ask yes/no" with Threshold 1.5 or NaN

    [Fact]
    public async Task AskYesNo_WithThresholdOutOfRange_FailsWithValidation()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(
            YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?", Threshold = 1.5 }), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task AskYesNo_WithThresholdOutOfRange_DoesNotCallProvider()
    {
        SetupBinary(0.9);

        await CreateYesNo().ExecuteAsync(
            YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?", Threshold = 1.5 }), CancellationToken.None);

        _decisionServiceMock.Verify(
            s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AskYesNo_WithThresholdNaN_FailsWithValidation()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(
            YesNoContext(new AskYesNoDecisionSettings { Instructions = "Is this spam?", Threshold = double.NaN }), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    #region Scenario: "Ask pick-one"/"Ask score" Confidence is empty when the provider reports none

    [Fact]
    public async Task AskChoice_OutputsNullConfidence_WhenProviderGivesNone()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Choice("a", null));

        var result = await CreateChoice().ExecuteAsync(
            ChoiceContext(new AskChoiceDecisionSettings
            {
                Instructions = "Pick",
                Options = [new AskChoiceDecisionOption { Key = "a" }, new AskChoiceDecisionOption { Key = "b" }],
            }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskChoiceDecisionOutput>().Confidence.ShouldBeNull();
    }

    [Fact]
    public async Task AskScore_OutputsNullConfidence_WhenProviderGivesNone()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIScoreDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Score(1.0, null));

        var result = await CreateScore().ExecuteAsync(
            ScoreContext(new AskScoreDecisionSettings { Instructions = "Rate", Levels = ["low", "high"] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskScoreDecisionOutput>().Confidence.ShouldBeNull();
    }

    #endregion

    #region Scenario: "Ask score" picks the nearest of three levels, rounding midpoints away from zero

    [Fact]
    public async Task AskScore_WithThreeLevels_RoundsDownToNearestLevel()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIScoreDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Score(1.4, null));

        var result = await CreateScore().ExecuteAsync(
            ScoreContext(new AskScoreDecisionSettings { Instructions = "Rate", Levels = ["low", "mid", "high"] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskScoreDecisionOutput>().Level.ShouldBe("mid");
    }

    [Fact]
    public async Task AskScore_WithThreeLevels_RoundsUpToNearestLevel()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIScoreDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Score(1.6, null));

        var result = await CreateScore().ExecuteAsync(
            ScoreContext(new AskScoreDecisionSettings { Instructions = "Rate", Levels = ["low", "mid", "high"] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskScoreDecisionOutput>().Level.ShouldBe("high");
    }

    [Fact]
    public async Task AskScore_AtAMidpoint_RoundsAwayFromZero()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIScoreDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Score(0.5, null));

        var result = await CreateScore().ExecuteAsync(
            ScoreContext(new AskScoreDecisionSettings { Instructions = "Rate", Levels = ["low", "mid", "high"] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskScoreDecisionOutput>().Level.ShouldBe("mid");
    }

    #endregion

    private static AIDecisionResponse<AIBinaryDecisionAnswer> Binary(double trueProbability)
    {
        var answer = new AIBinaryDecisionAnswer { TrueProbability = trueProbability };
        return new AIDecisionResponse<AIBinaryDecisionAnswer>
        {
            Answer = answer,
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = answer },
        };
    }

    private static AIDecisionResponse<AIChoiceDecisionAnswer> Choice(string choice, double? confidence)
    {
        var answer = new AIChoiceDecisionAnswer
        {
            Choice = choice,
            Probabilities = new Dictionary<string, double>(),
            Confidence = confidence,
        };
        return new AIDecisionResponse<AIChoiceDecisionAnswer>
        {
            Answer = answer,
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = answer },
        };
    }

    private static AIDecisionResponse<AIScoreDecisionAnswer> Score(double score, double? confidence)
    {
        var answer = new AIScoreDecisionAnswer
        {
            Score = score,
            Probabilities = new Dictionary<int, double>(),
            Confidence = confidence,
        };
        return new AIDecisionResponse<AIScoreDecisionAnswer>
        {
            Answer = answer,
            Answers = new Dictionary<string, AIDecisionAnswer> { ["q"] = answer },
        };
    }

    private void SetupBinary(double trueProbability)
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Binary(trueProbability));

    private AskYesNoDecisionAction CreateYesNo()
        => new(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskYesNoDecisionAction>>());

    private AskChoiceDecisionAction CreateChoice()
        => new(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskChoiceDecisionAction>>());

    private AskScoreDecisionAction CreateScore()
        => new(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskScoreDecisionAction>>());

    private static ActionContext YesNoContext(AskYesNoDecisionSettings settings)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.AskYesNoDecision,
            Settings = settings,
        };

    private static ActionContext ChoiceContext(AskChoiceDecisionSettings settings)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision,
            Settings = settings,
        };

    private static ActionContext ScoreContext(AskScoreDecisionSettings settings)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.AskScoreDecision,
            Settings = settings,
        };
}
