#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

using Umbraco.AI.Core;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Tests.Unit.Services;

/// <summary>
/// PLAN.md T8's added requirement: proves T7's wrapping order (<see cref="ValidatingDecisionClient"/>
/// outermost) actually holds at the real entry point a caller uses —
/// <see cref="IAIDecisionService.AskAsync"/> — not just at the factory's own seam
/// (<c>AIDecisionClientFactoryTests</c> already covers that). Uses the real
/// <see cref="AIDecisionClientFactory"/>/pipeline underneath <see cref="AIDecisionService"/>; only
/// <see cref="IAIProfileService"/>, <see cref="IAIConnectionService"/>, the runtime-context accessor/scope
/// provider, and the event aggregator are mocked (see <see cref="ArrangeService"/>).
/// </summary>
public class AIDecisionServiceRealPipelineTests
{
    private const string ProviderId = "fake-provider";
    private const string ModelId = "fake-model-1";
    private const string ProfileAlias = "spam-check";

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_ThrowsArgumentException()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var (service, _, _, _, _, _) = ArrangeService(throwingClient);
        var invalidQuestion = new AIDecisionQuestion
        {
            Kind = AIDecisionKind.Choice,
            Prompt = "pick one",
            Choices = ["only-one"],
        };

        // Act
        var act = () => service.AskAsync(ProfileAlias, invalidQuestion);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_NeverInvokesProviderClient()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var (service, _, _, _, _, _) = ArrangeService(throwingClient);
        var invalidQuestion = new AIDecisionQuestion
        {
            Kind = AIDecisionKind.Choice,
            Prompt = "pick one",
            Choices = ["only-one"],
        };

        // Act
        await Should.ThrowAsync<ArgumentException>(
            () => service.AskAsync(ProfileAlias, invalidQuestion));

        // Assert — ValidatingDecisionClient (outermost, wrapped by the real AIDecisionClientFactory)
        // rejected the question before it ever reached the provider's client.
        throwingClient.ReceivedRequests.ShouldBeEmpty();
    }

    /// <summary>
    /// T8 Finding 2 — the inline builder-based entry point
    /// (<see cref="IAIDecisionService.AskAsync(Action{AIDecisionBuilder}, AIDecisionQuestion, CancellationToken)"/>)
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
        var respondingClient = new FakeDecisionClient(_ => AIDecisionResponse.ForBinary(true, 0.87));
        var (service, eventAggregatorMock, context, _, _, _) = ArrangeService(respondingClient);
        var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" };

        // Act
        var response = await service.AskAsync(
            b => b.WithAlias("inline-spam-check").WithProfile(ProfileAlias),
            question);

        // Assert
        response.BinaryAnswer.ShouldBe(true);
        eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutingNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutedNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe(Constants.FeatureTypes.InlineDecision);
        context.GetValue<string>(Constants.ContextKeys.FeatureAlias).ShouldBe("inline-spam-check");
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
        var respondingClient = new FakeDecisionClient(_ => AIDecisionResponse.ForBinary(true, 0.87));
        var (service, eventAggregatorMock, context, _, _, _) = ArrangeService(respondingClient);
        var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" };

        // Act
        await service.AskAsync(
            b => b.WithAlias("passthrough-spam-check").WithProfile(ProfileAlias).AsPassThrough(),
            question);

        // Assert
        eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutingNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIDecisionExecutedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBeNull();
        context.GetValue<string>(Constants.ContextKeys.FeatureAlias).ShouldBeNull();
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
        var respondingClient = new FakeDecisionClient(_ => AIDecisionResponse.ForBinary(true, 0.87));
        var (service, _, _, _, contextAccessorMock, scopeProviderMock) = ArrangeService(respondingClient);

        // Simulate a parent scope already open (e.g. an agent run) before this call is made.
        var parentContext = new AIRuntimeContext([]);
        contextAccessorMock.Setup(x => x.Context).Returns(parentContext);

        var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" };

        // Act
        await service.AskAsync(
            b => b.WithAlias("nested-spam-check").WithProfile(ProfileAlias),
            question);

        // Assert
        parentContext.GetValue<string>(Constants.ContextKeys.FeatureType).ShouldBe(Constants.FeatureTypes.InlineDecision);
        parentContext.GetValue<string>(Constants.ContextKeys.FeatureAlias).ShouldBe("nested-spam-check");
        scopeProviderMock.Verify(
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
        var respondingClient = new FakeDecisionClient(_ => AIDecisionResponse.ForBinary(true, 0.87));
        var (service, _, _, profileServiceMock, _, _) = ArrangeService(respondingClient);
        var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" };

        // Act
        var response = await service.AskAsync(b => b.WithAlias("default-profile-check"), question);

        // Assert
        response.BinaryAnswer.ShouldBe(true);
        profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Mirrors <c>AISpeechToTextServiceTests.TranscribeAsync_WithChatProfile_ThrowsInvalidOperationException</c>
    /// — a resolved profile whose <see cref="AIProfile.Capability"/> isn't <see cref="AICapability.Decision"/>
    /// is rejected before any provider client is reached.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithBuilder_ProfileWithWrongCapability_ThrowsInvalidOperationException()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached when the resolved profile is the wrong capability."));
        var (service, _, _, _, _, _) = ArrangeService(throwingClient, profileCapability: AICapability.Chat);
        var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" };

        // Act
        var act = () => service.AskAsync(
            b => b.WithAlias("wrong-capability-check").WithProfile(ProfileAlias),
            question);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support decision capability");
    }

    /// <summary>
    /// Wires a real <see cref="AIDecisionClientFactory"/> (same shape as
    /// <c>AIDecisionClientFactoryTests.ArrangeFactory</c>) behind an <see cref="AIDecisionService"/>. The
    /// profile service, connection service, runtime-context accessor/scope provider, and event aggregator
    /// are all mocked; everything else in the pipeline is real.
    /// </summary>
    /// <param name="innerClient">The provider-level decision client to sit under the real pipeline.</param>
    /// <param name="profileCapability">
    /// The capability declared on the resolved profile. Defaults to <see cref="AICapability.Decision"/>;
    /// pass a different capability to exercise <see cref="AIDecisionService"/>'s capability-mismatch
    /// rejection.
    /// </param>
    private static (
        AIDecisionService Service,
        Mock<IEventAggregator> EventAggregatorMock,
        AIRuntimeContext Context,
        Mock<IAIProfileService> ProfileServiceMock,
        Mock<IAIRuntimeContextAccessor> ContextAccessorMock,
        Mock<IAIRuntimeContextScopeProvider> ScopeProviderMock) ArrangeService(
            IAIDecisionClient innerClient,
            AICapability profileCapability = AICapability.Decision)
    {
        var connectionId = Guid.NewGuid();
        var connectionSettings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId)
            .WithProviderId(ProviderId)
            .WithSettings(connectionSettings)
            .IsActive(true)
            .Build();

        var profile = new AIProfileBuilder()
            .WithConnectionId(connectionId)
            .WithModel(ProviderId, ModelId)
            .WithCapability(profileCapability)
            .WithAlias(ProfileAlias)
            .Build();

        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var capability = new SingleSettingsDecisionCapability(provider, innerClient);
        var configuredCapability = new AIConfiguredDecisionCapability(capability, connectionSettings);

        var configuredProviderMock = new Mock<IAIConfiguredProvider>();
        configuredProviderMock.Setup(x => x.Provider).Returns(provider);
        configuredProviderMock.Setup(x => x.GetCapability<IAIConfiguredDecisionCapability>()).Returns(configuredCapability);

        var connectionServiceMock = new Mock<IAIConnectionService>();
        connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        var contextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        var scopeProviderMock = new Mock<IAIRuntimeContextScopeProvider>();
        var contributors = new AIRuntimeContextContributorCollection(Enumerable.Empty<IAIRuntimeContextContributor>);

        // ScopedProfileDecisionClient opens a runtime-context scope per call when none exists already;
        // give it a real context so it doesn't fault before the request under test runs.
        var context = new AIRuntimeContext([]);
        var scope = new Mock<IAIRuntimeContextScope>();
        scope.Setup(x => x.Context).Returns(context);
        contextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);
        scopeProviderMock
            .Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()))
            .Returns(() =>
            {
                contextAccessorMock.Setup(x => x.Context).Returns(context);
                return scope.Object;
            });

        var factory = new AIDecisionClientFactory(
            connectionServiceMock.Object,
            new AIDecisionMiddlewareCollection(Enumerable.Empty<IAIDecisionMiddleware>),
            contextAccessorMock.Object,
            scopeProviderMock.Object,
            contributors,
            new Mock<IAIEditableModelResolver>().Object);

        var profileServiceMock = new Mock<IAIProfileService>();
        profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(ProfileAlias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        profileServiceMock
            .Setup(x => x.GetProfileAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var eventAggregatorMock = new Mock<IEventAggregator>();
        eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Same accessor/scope-provider/contributors instances as the factory above — mirrors how DI
        // shares these as singletons between AIDecisionClientFactory and AIDecisionService in production.
        var service = new AIDecisionService(
            profileServiceMock.Object,
            factory,
            eventAggregatorMock.Object,
            contextAccessorMock.Object,
            scopeProviderMock.Object,
            contributors);

        return (service, eventAggregatorMock, context, profileServiceMock, contextAccessorMock, scopeProviderMock);
    }

    /// <summary>The minimal real <see cref="IAIDecisionCapability"/> a provider package would define,
    /// with no provider-declared capability settings.</summary>
    private sealed class SingleSettingsDecisionCapability(IAIProvider provider, IAIDecisionClient inner)
        : AIDecisionCapabilityBase<FakeProviderSettings>(provider)
    {
        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override IAIDecisionClient CreateClient(FakeProviderSettings settings, string? modelId)
            => inner;
    }
}
