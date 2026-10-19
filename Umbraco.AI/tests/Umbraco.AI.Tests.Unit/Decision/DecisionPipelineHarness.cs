#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// Shared arrange for DR-1 specs: extracted from AIDecisionServiceRealPipelineTests.ArrangeService.
// T5 switched that file to use this harness too, rather than keep a second copy — see the
// EventAggregatorMock/Context/ContextAccessorMock/ScopeProviderMock properties below, added for its
// notification/scope-management assertions that AskTypedDecisionTests doesn't need.

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

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// A real <see cref="AIDecisionService"/> over the real <see cref="AIDecisionClientFactory"/> pipeline.
/// Only the profile/connection services, runtime-context accessor/scope provider and event aggregator
/// are mocked; the provider client is whatever the spec passes in.
/// </summary>
internal sealed class DecisionPipelineHarness
{
    public const string ProviderId = "fake-provider";
    public const string ModelId = "fake-model-1";
    public const string ProfileAlias = "spam-check";

    public DecisionPipelineHarness(
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

        Profile = new AIProfileBuilder()
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
        configuredProviderMock
            .Setup(x => x.GetCapability<IAIConfiguredDecisionCapability>())
            .Returns(configuredCapability);

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

        ProfileServiceMock = new Mock<IAIProfileService>();
        ProfileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(ProfileAlias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile);
        ProfileServiceMock
            .Setup(x => x.GetProfileAsync(Profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile);
        ProfileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Decision, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile);

        EventAggregatorMock = new Mock<IEventAggregator>();
        EventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Context = context;
        ContextAccessorMock = contextAccessorMock;
        ScopeProviderMock = scopeProviderMock;

        Service = new AIDecisionService(
            ProfileServiceMock.Object,
            factory,
            EventAggregatorMock.Object,
            contextAccessorMock.Object,
            scopeProviderMock.Object,
            contributors);
    }

    public AIDecisionService Service { get; }

    public AIProfile Profile { get; }

    public Mock<IAIProfileService> ProfileServiceMock { get; }

    /// <summary>The event aggregator mock the service publishes executing/executed notifications to.</summary>
    public Mock<IEventAggregator> EventAggregatorMock { get; }

    /// <summary>
    /// The runtime context a scope created via <see cref="ScopeProviderMock"/> hands back — where feature
    /// metadata (<c>FeatureType</c>/<c>FeatureAlias</c>) ends up stamped once a scope has been created.
    /// </summary>
    public AIRuntimeContext Context { get; }

    public Mock<IAIRuntimeContextAccessor> ContextAccessorMock { get; }

    public Mock<IAIRuntimeContextScopeProvider> ScopeProviderMock { get; }

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
