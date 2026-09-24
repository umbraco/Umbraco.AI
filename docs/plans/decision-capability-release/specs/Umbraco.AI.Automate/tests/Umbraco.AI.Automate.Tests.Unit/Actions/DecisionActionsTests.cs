// DR-9 — Branch automations on a decision
//
// ASSUMPTION (T20 builder confirms/adjusts): action constructors take
// (ActionInfrastructure, IAIDecisionService, IAIExperimentalFeatures, ILogger<T>), mirroring
// TranscribeAudioAction; IAIDecisionService exposes
// AskAsync<TResponse>(Action<AIDecisionBuilder>, AIDecisionQuestion<TResponse>, CancellationToken).
// The Options/Levels setting types depend on T20's field-editor choice.
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

public class DecisionActionsTests
{
    private readonly Mock<IAIDecisionService> _decisionServiceMock = new();
    private readonly Mock<IAIExperimentalFeatures> _experimentalMock = new();
    private readonly ActionInfrastructure _infrastructure = new(new Mock<IEditableModelResolver>().Object);

    public DecisionActionsTests()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(true);
    }

    #region Scenario: "Ask yes/no" and the provider answers probability 0.9

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_Succeeds()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.Status.ShouldBe(ActionResultStatus.Success);
    }

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_OutputsAnswerTrue()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Answer.ShouldBeTrue();
    }

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_OutputsProbability()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Probability.ShouldBe(0.9);
    }

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_OutputsConfidence()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Confidence.ShouldBe(0.9);
    }

    #endregion

    #region Scenario: "Ask pick-one" with options a, b and the provider picks b

    [Fact(Skip = "Pending T20")]
    public async Task AskChoice_OutputsChosenKey()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIChoiceDecisionResponse { Choice = "b", ChoiceConfidence = 0.8 });
        var action = new AskChoiceDecisionAction(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskChoiceDecisionAction>>());

        var result = await action.ExecuteAsync(Context(UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision,
            new AskChoiceDecisionSettings { Instructions = "Pick", Options = [new("a"), new("b")] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskChoiceDecisionOutput>().Choice.ShouldBe("b");
    }

    #endregion

    #region Scenario: "Ask score" with levels low, high and the provider returns 1.0

    [Fact(Skip = "Pending T20")]
    public async Task AskScore_OutputsLevel()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIScoreDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIScoreDecisionResponse { Score = 1.0, Level = "high", ScoreConfidence = 0.7 });
        var action = new AskScoreDecisionAction(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskScoreDecisionAction>>());

        var result = await action.ExecuteAsync(Context(UmbracoAIAutomateConstants.ActionTypes.AskScoreDecision,
            new AskScoreDecisionSettings { Instructions = "Rate", Levels = ["low", "high"] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskScoreDecisionOutput>().Level.ShouldBe("high");
    }

    #endregion

    #region Scenario: ProfileId is empty

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_WithoutProfileId_DoesNotSetProfileOnBuilder()
    {
        Action<AIDecisionBuilder>? captured = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIBinaryDecisionQuestion, CancellationToken>((b, _, _) => captured = b)
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.9 });

        await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        var builder = new AIDecisionBuilder();
        captured!(builder);
        builder.ProfileId.ShouldBeNull(); // null = default Decision profile; property name per AIDecisionBuilder
    }

    #endregion

    #region Scenario: Instructions come from a bound value

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_SendsResolvedInstructions()
    {
        AIBinaryDecisionQuestion? sent = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIBinaryDecisionQuestion, CancellationToken>((_, q, _) => sent = q)
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.9 });

        await CreateYesNo().ExecuteAsync(YesNoContext(instructions: "value from earlier step"), CancellationToken.None);

        sent!.Instructions.ShouldBe("value from earlier step");
    }

    #endregion

    #region Sad path: flag turned off after startup

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_WhenFlagOff_FailsWithValidation()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_WhenFlagOff_DoesNotCallProvider()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        _decisionServiceMock.Verify(
            s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Sad path: "Ask pick-one" with 1 option

    [Fact(Skip = "Pending T20")]
    public async Task AskChoice_WithOneOption_FailsWithValidation()
    {
        var action = new AskChoiceDecisionAction(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskChoiceDecisionAction>>());

        var result = await action.ExecuteAsync(Context(UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision,
            new AskChoiceDecisionSettings { Instructions = "Pick", Options = [new("a")] }), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    #region Sad path: provider throws

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_WhenProviderThrows_FailsWithUnknown()
    {
        SetupBinaryThrows("Jev is down");

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Unknown);
    }

    [Fact(Skip = "Pending T20")]
    public async Task AskYesNo_WhenProviderThrows_SurfacesProviderMessage()
    {
        SetupBinaryThrows("Jev is down");

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.ErrorMessage.ShouldContain("Jev is down"); // property name per ActionResult
    }

    #endregion

    private void SetupBinary(double probability)
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = probability });

    private void SetupBinaryThrows(string message)
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(message));

    private AskYesNoDecisionAction CreateYesNo()
        => new(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskYesNoDecisionAction>>());

    private static ActionContext YesNoContext(string instructions = "Is this spam?")
        => Context(UmbracoAIAutomateConstants.ActionTypes.AskYesNoDecision, new AskYesNoDecisionSettings { Instructions = instructions });

    private static ActionContext Context(string alias, object settings)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = alias,
            Settings = settings,
        };
}
