// DR-9 — Branch automations on a decision
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Providers.Errors;
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

    [Fact]
    public async Task AskYesNo_Succeeds()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.Status.ShouldBe(ActionResultStatus.Success);
    }

    [Fact]
    public async Task AskYesNo_OutputsAnswerTrue()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Answer.ShouldBeTrue();
    }

    [Fact]
    public async Task AskYesNo_OutputsProbability()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Probability.ShouldBe(0.9);
    }

    [Fact]
    public async Task AskYesNo_OutputsConfidence()
    {
        SetupBinary(0.9);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskYesNoDecisionOutput>().Confidence.ShouldBe(0.9);
    }

    #endregion

    #region Scenario: "Ask pick-one" with options a, b and the provider picks b

    [Fact]
    public async Task AskChoice_OutputsChosenKey()
    {
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIChoiceDecisionResponse { Choice = "b", ChoiceConfidence = 0.8 });
        var action = new AskChoiceDecisionAction(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskChoiceDecisionAction>>());

        var result = await action.ExecuteAsync(Context(UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision,
            new AskChoiceDecisionSettings { Instructions = "Pick", Options = [new AskChoiceDecisionOption { Key = "a" }, new AskChoiceDecisionOption { Key = "b" }] }), CancellationToken.None);

        result.OutputData.ShouldBeOfType<AskChoiceDecisionOutput>().Choice.ShouldBe("b");
    }

    #endregion

    #region Scenario: "Ask score" with levels low, high and the provider returns 1.0

    [Fact]
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

    [Fact]
    public async Task AskYesNo_WithoutProfileId_DoesNotSetProfileOnBuilder()
    {
        Action<AIDecisionBuilder>? captured = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            // Callback's generic parameters must match AskAsync<TResponse>'s actual parameter
            // types (AIDecisionQuestion<AIBinaryDecisionResponse>), not the narrower argument
            // type used in the Setup's It.IsAny<> above.
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIBinaryDecisionResponse>, CancellationToken>((b, _, _) => captured = b)
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.9 });

        await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        var builder = new AIDecisionBuilder();
        captured!(builder);
        // ProfileId is internal to Umbraco.AI.Core (not visible to this assembly), so read the
        // backing field via reflection, mirroring TranscribeAudioActionTests' GetPrivateField.
        GetPrivateField<Guid?>(builder, "_profileId").ShouldBeNull(); // null = default Decision profile
    }

    #endregion

    #region Scenario: Instructions come from a bound value

    [Fact]
    public async Task AskYesNo_SendsResolvedInstructions()
    {
        AIDecisionQuestion<AIBinaryDecisionResponse>? sent = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIBinaryDecisionResponse>, CancellationToken>((_, q, _) => sent = q)
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.9 });

        await CreateYesNo().ExecuteAsync(YesNoContext(instructions: "value from earlier step"), CancellationToken.None);

        sent!.Instructions.ShouldBe("value from earlier step");
    }

    #endregion

    #region Scenario: True/false criteria can be bound to an upstream action's output

    [Fact]
    public void TrueCriteria_SupportsBindings()
    {
        var field = GetField<AskYesNoDecisionSettings>(nameof(AskYesNoDecisionSettings.TrueCriteria));

        field.SupportsBindings.ShouldBeTrue();
    }

    [Fact]
    public void FalseCriteria_SupportsBindings()
    {
        var field = GetField<AskYesNoDecisionSettings>(nameof(AskYesNoDecisionSettings.FalseCriteria));

        field.SupportsBindings.ShouldBeTrue();
    }

    private static FieldAttribute GetField<TSettings>(string propertyName)
    {
        var property = typeof(TSettings).GetProperty(propertyName);
        property.ShouldNotBeNull($"Expected property '{propertyName}' on {typeof(TSettings).Name}.");
        var attribute = property.GetCustomAttribute<FieldAttribute>();
        attribute.ShouldNotBeNull($"Expected [Field] attribute on {typeof(TSettings).Name}.{propertyName}.");
        return attribute;
    }

    #endregion

    #region Sad path: flag turned off after startup

    [Fact]
    public async Task AskYesNo_WhenFlagOff_FailsWithValidation()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task AskYesNo_WhenFlagOff_DoesNotCallProvider()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        _decisionServiceMock.Verify(
            s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Sad path: "Ask pick-one" with 1 option (rejected by Core's Decision validator)

    [Fact]
    public async Task AskChoice_WithOneOption_FailsWithValidation()
    {
        // Core's ValidatingDecisionClient (via the shared DecisionQuestionValidator) is the single
        // source of the 2..255 option-count rule, and throws ArgumentException before any provider
        // call — the action just maps that to Validation, same as it does for any other
        // structurally invalid question Core rejects.
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Options must contain between 2 and 255 entries.", "question"));
        var action = new AskChoiceDecisionAction(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskChoiceDecisionAction>>());

        var result = await action.ExecuteAsync(Context(UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision,
            new AskChoiceDecisionSettings { Instructions = "Pick", Options = [new AskChoiceDecisionOption { Key = "a" }] }), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    #region Scenario: MapOptions maps the Options key/value list into AIDecisionOption entries

    [Fact]
    public async Task AskChoice_MapsKeyAndValue_ToKeyAndDescription()
    {
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = "a", Value = "Apple" });

        sent!.Options[0].ShouldBe(new AIDecisionOption("a", "Apple"));
    }

    [Fact]
    public async Task AskChoice_MapsBlankValue_ToNullDescription()
    {
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = "a", Value = "  " });

        sent!.Options[0].ShouldBe(new AIDecisionOption("a", null));
    }

    [Fact]
    public async Task AskChoice_MapsNullValue_ToNullDescription()
    {
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = "a", Value = null });

        sent!.Options[0].ShouldBe(new AIDecisionOption("a", null));
    }

    [Fact]
    public async Task AskChoice_TrimsSurroundingWhitespace_FromKey()
    {
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = "  a  ", Value = "Apple" });

        sent!.Options[0].Key.ShouldBe("a");
    }

    [Fact]
    public async Task AskChoice_TrimsSurroundingWhitespace_FromValue()
    {
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = "a", Value = "  Apple  " });

        sent!.Options[0].Description.ShouldBe("Apple");
    }

    [Fact]
    public async Task AskChoice_PreservesOrder()
    {
        var sent = await CaptureChoiceQuestionAsync(
            new AskChoiceDecisionOption { Key = "b" },
            new AskChoiceDecisionOption { Key = "a" });

        sent!.Options.ShouldBe([new AIDecisionOption("b"), new AIDecisionOption("a")]);
    }

    [Fact]
    public async Task AskChoice_PassesThroughDuplicateKeys_Unchanged()
    {
        var sent = await CaptureChoiceQuestionAsync(
            new AskChoiceDecisionOption { Key = "a" },
            new AskChoiceDecisionOption { Key = "a" });

        sent!.Options.ShouldBe([new AIDecisionOption("a"), new AIDecisionOption("a")]);
    }

    [Fact]
    public async Task AskChoice_PassesThroughEmptyKey_ForCoreToReject()
    {
        // MapOptions itself doesn't reject an empty key -- that's left to Core's Decision
        // validator downstream, once the question reaches IAIDecisionService.
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = "", Value = "desc" });

        sent!.Options[0].ShouldBe(new AIDecisionOption("", "desc"));
    }

    [Fact]
    public async Task AskChoice_MapsNullOptionEntry_ToEmptyKey()
    {
        // A null entry in Options (e.g. hand-edited settings JSON) must not throw a
        // NullReferenceException -- it maps to an empty key so Core's Decision validator rejects
        // it downstream as Validation, same as any other blank key.
        var sent = await CaptureChoiceQuestionAsync(null!, new AskChoiceDecisionOption { Key = "b" });

        sent!.Options[0].ShouldBe(new AIDecisionOption("", null));
    }

    [Fact]
    public async Task AskChoice_MapsNullKey_ToEmptyKey()
    {
        // A "key": null entry deserializes AskChoiceDecisionOption.Key to null despite its
        // string.Empty default, so this must not throw either.
        var sent = await CaptureChoiceQuestionAsync(new AskChoiceDecisionOption { Key = null!, Value = "desc" });

        sent!.Options[0].ShouldBe(new AIDecisionOption("", "desc"));
    }

    private async Task<AIChoiceDecisionQuestion?> CaptureChoiceQuestionAsync(params AskChoiceDecisionOption[] options)
    {
        AIChoiceDecisionQuestion? sent = null;
        _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIChoiceDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionQuestion<AIChoiceDecisionResponse>, CancellationToken>((_, q, _) => sent = (AIChoiceDecisionQuestion)q)
            .ReturnsAsync(new AIChoiceDecisionResponse { Choice = "a", ChoiceConfidence = 0.5 });
        var action = new AskChoiceDecisionAction(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskChoiceDecisionAction>>());

        await action.ExecuteAsync(Context(UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision,
            new AskChoiceDecisionSettings { Instructions = "Pick", Options = [.. options] }), CancellationToken.None);

        return sent;
    }

    #endregion

    #region Sad path: provider throws

    [Fact]
    public async Task AskYesNo_WhenProviderThrows_FailsWithUnknown()
    {
        SetupBinaryThrows("Jev is down");

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Unknown);
    }

    [Fact]
    public async Task AskYesNo_WhenProviderThrows_SurfacesProviderMessage()
    {
        SetupBinaryThrows("Jev is down");

        var result = await CreateYesNo().ExecuteAsync(YesNoContext(), CancellationToken.None);

        result.Exception!.Message.ShouldContain("Jev is down"); // ActionResult exposes Exception, not ErrorMessage
    }

    #endregion

    private void SetupBinary(double probability)
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIBinaryDecisionResponse { Probability = probability });

    private void SetupBinaryThrows(string message)
        => _decisionServiceMock
            .Setup(s => s.AskAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIBinaryDecisionQuestion>(), It.IsAny<CancellationToken>()))
            // AIProviderException (not InvalidOperationException, which the action maps to
            // Validation for "no default profile") is what a real provider failure surfaces as.
            .ThrowsAsync(new AIProviderException(new AIProviderErrorInfo(AIProviderErrorCategory.Unknown, message, null, message)));

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

    private static T? GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.ShouldNotBeNull($"Expected private field '{fieldName}' on {instance.GetType().Name}.");
        return (T?)field.GetValue(instance);
    }
}
