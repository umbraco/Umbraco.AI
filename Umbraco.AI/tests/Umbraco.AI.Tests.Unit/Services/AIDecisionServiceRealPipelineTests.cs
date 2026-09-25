#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

using Umbraco.AI.Core;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Tests.Unit.Decision;

namespace Umbraco.AI.Tests.Unit.Services;

/// <summary>
/// PLAN.md T8's added requirement: proves T7's wrapping order (<see cref="ValidatingDecisionClient"/>
/// outermost) actually holds at the real entry point a caller uses —
/// <see cref="IAIDecisionService.AskAsync{TResponse}(string, AIDecisionQuestion{TResponse}, AIDecisionOptions?, CancellationToken)"/>
/// — not just at the factory's own seam (<c>AIDecisionClientFactoryTests</c> already covers that). Uses
/// the real <see cref="AIDecisionClientFactory"/>/pipeline underneath <see cref="AIDecisionService"/> via
/// <see cref="DecisionPipelineHarness"/> — only <see cref="Umbraco.AI.Core.Profiles.IAIProfileService"/>,
/// <see cref="Umbraco.AI.Core.Connections.IAIConnectionService"/>, the runtime-context accessor/scope
/// provider, and the event aggregator are mocked (see the harness).
/// </summary>
public class AIDecisionServiceRealPipelineTests
{
    private const string ProfileAlias = DecisionPipelineHarness.ProfileAlias;

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_ThrowsArgumentException()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var harness = new DecisionPipelineHarness(throwingClient);
        var invalidQuestion = new AIChoiceDecisionQuestion
        {
            Instructions = "pick one",
            Options = [new AIDecisionOption("only-one")],
        };

        // Act
        var act = () => harness.Service.AskAsync(ProfileAlias, invalidQuestion);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_NeverInvokesProviderClient()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var harness = new DecisionPipelineHarness(throwingClient);
        var invalidQuestion = new AIChoiceDecisionQuestion
        {
            Instructions = "pick one",
            Options = [new AIDecisionOption("only-one")],
        };

        // Act
        await Should.ThrowAsync<ArgumentException>(
            () => harness.Service.AskAsync(ProfileAlias, invalidQuestion));

        // Assert — ValidatingDecisionClient (outermost, wrapped by the real AIDecisionClientFactory)
        // rejected the question before it ever reached the provider's client.
        throwingClient.ReceivedRequests.ShouldBeEmpty();
    }

    /// <summary>
    /// T8 Finding 2 — the inline builder-based entry point
    /// (<see cref="IAIDecisionService.AskAsync{TResponse}(Action{AIDecisionBuilder}, AIDecisionQuestion{TResponse}, CancellationToken)"/>)
    /// end-to-end through the same real <see cref="AIDecisionClientFactory"/> pipeline as the sad-path
    /// tests above, proving <see cref="ScopedInlineDecisionClient"/> (T7's previously-unconsumed type) is
    /// actually wired in — not just that a response comes back, but that it actually stamped feature
    /// metadata onto the shared <see cref="AIRuntimeContext"/> — and that both inline-execution
    /// notifications are published.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithBuilder_PublishesNotificationsAndReturnsResponse()
    {
        // Arrange
        var respondingClient = new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.87 });
        var harness = new DecisionPipelineHarness(respondingClient);
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        var response = await harness.Service.AskAsync(
            b => b.WithAlias("inline-spam-check").WithProfile(ProfileAlias),
            question);

        // Assert
        response.Answer.ShouldBe(true);
        harness.EventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutingNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        harness.EventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutedNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        harness.Context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe(Constants.FeatureTypes.InlineDecision);
        harness.Context.GetValue<string>(Constants.ContextKeys.FeatureAlias).ShouldBe("inline-spam-check");
    }

    /// <summary>
    /// T8 Finding 1 — a pass-through execution (<see cref="AIDecisionBuilder.AsPassThrough"/>) must skip
    /// both notifications and feature metadata, even when (as here) no parent scope already exists. This
    /// is exactly the case the bug got backwards: <see cref="ScopedInlineDecisionClient"/> used to decide
    /// whether to stamp metadata from "did a parent scope already exist" rather than "is this
    /// pass-through", so a pass-through call with no parent scope wrongly stamped metadata anyway.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithBuilder_AsPassThrough_SkipsNotificationsAndFeatureMetadata()
    {
        // Arrange
        var respondingClient = new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.87 });
        var harness = new DecisionPipelineHarness(respondingClient);
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        await harness.Service.AskAsync(
            b => b.WithAlias("passthrough-spam-check").WithProfile(ProfileAlias).AsPassThrough(),
            question);

        // Assert
        harness.EventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutingNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        harness.EventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        harness.Context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBeNull();
        harness.Context.GetValue<string>(Constants.ContextKeys.FeatureAlias).ShouldBeNull();
    }

    /// <summary>
    /// T8 review round 3 finding — a normal (non-pass-through) call made from inside an already-existing
    /// parent scope (e.g. a decision asked mid agent-run) must still stamp feature metadata, and must not
    /// create a new scope since one already exists. This is the case pass-2's fix got backwards in the
    /// other direction: keying the decision off <c>!scopeExisted</c> instead of <c>!builder.IsPassThrough</c>
    /// would wrongly skip metadata here.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithBuilder_CalledFromExistingScope_StampsMetadataWithoutCreatingNewScope()
    {
        // Arrange
        var respondingClient = new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.87 });
        var harness = new DecisionPipelineHarness(respondingClient);

        // Simulate a parent scope already open (e.g. an agent run) before this call is made.
        var parentContext = new AIRuntimeContext([]);
        harness.ContextAccessorMock.Setup(x => x.Context).Returns(parentContext);

        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        await harness.Service.AskAsync(
            b => b.WithAlias("nested-spam-check").WithProfile(ProfileAlias),
            question);

        // Assert
        parentContext.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe(Constants.FeatureTypes.InlineDecision);
        parentContext.GetValue<string>(Constants.ContextKeys.FeatureAlias).ShouldBe("nested-spam-check");
        harness.ScopeProviderMock.Verify(
            x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()),
            Times.Never);
    }

    /// <summary>
    /// Mirrors <c>AISpeechToTextServiceTests.TranscribeAsync_WithDefaultProfile_UsesDefaultProfile</c> —
    /// a builder with no profile ID/alias configured falls back to the default Decision profile.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithBuilder_NoProfileConfigured_UsesDefaultProfile()
    {
        // Arrange
        var respondingClient = new FakeDecisionClient(_ => new AIBinaryDecisionResponse { Probability = 0.87 });
        var harness = new DecisionPipelineHarness(respondingClient);
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        var response = await harness.Service.AskAsync(b => b.WithAlias("default-profile-check"), question);

        // Assert
        response.Answer.ShouldBe(true);
        harness.ProfileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Mirrors <c>AISpeechToTextServiceTests.TranscribeAsync_WithChatProfile_ThrowsInvalidOperationException</c>
    /// — a resolved profile whose <see cref="Umbraco.AI.Core.Profiles.AIProfile.Capability"/> isn't
    /// <see cref="AICapability.Decision"/> is rejected before any provider client is reached.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithBuilder_ProfileWithWrongCapability_ThrowsInvalidOperationException()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached when the resolved profile is the wrong capability."));
        var harness = new DecisionPipelineHarness(throwingClient, profileCapability: AICapability.Chat);
        var question = new AIBinaryDecisionQuestion { Instructions = "is this spam?" };

        // Act
        var act = () => harness.Service.AskAsync(
            b => b.WithAlias("wrong-capability-check").WithProfile(ProfileAlias),
            question);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support decision capability");
    }
}
