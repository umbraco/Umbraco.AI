#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// Shared arrange for the Extraction service specs, copied from DecisionPipelineHarness.
//
// ASSUMPTIONS (builder confirms/adjusts, keeping each spec's behavior and single assertion):
// - AIExtractionService takes the same collaborators as AIDecisionService plus IAIExperimentalFeatures,
//   because SPEC.md requires the service itself to refuse while Umbraco:AI:Experimental:Extraction is off
//   (Decision's service has no such check).
// - AIExtractionClientFactory's constructor matches AIDecisionClientFactory's.

using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// A real <see cref="AIExtractionService"/> over the real <see cref="AIExtractionClientFactory"/> pipeline.
/// Only the profile/connection services, runtime-context accessor/scope provider and event aggregator
/// are mocked; the experimental flag is a real <see cref="AIExperimentalFeatures"/>; the provider client
/// is whatever the spec passes in.
/// </summary>
internal sealed class ExtractionPipelineHarness
{
    public const string ProviderId = "fake-provider";
    public const string ModelId = "fake-model-1";
    public const string ProfileAlias = "cloudflare-clairic";

    public ExtractionPipelineHarness(
        IAIExtractionClient innerClient,
        AICapability profileCapability = AICapability.Extraction,
        bool extractionEnabled = true)
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
        var capability = new SingleSettingsExtractionCapability(provider, innerClient);
        var configuredCapability = new AIConfiguredExtractionCapability(capability, connectionSettings);

        var configuredProviderMock = new Mock<IAIConfiguredProvider>();
        configuredProviderMock.Setup(x => x.Provider).Returns(provider);
        configuredProviderMock
            .Setup(x => x.GetCapability<IAIConfiguredExtractionCapability>())
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

        var factory = new AIExtractionClientFactory(
            connectionServiceMock.Object,
            new AIExtractionMiddlewareCollection(Enumerable.Empty<IAIExtractionMiddleware>),
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
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Extraction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile);

        EventAggregatorMock = new Mock<IEventAggregator>();
        EventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var experimentalOptions = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
        experimentalOptions
            .Setup(x => x.CurrentValue)
            .Returns(new AIExperimentalOptions { Extraction = extractionEnabled });

        Context = context;
        ContextAccessorMock = contextAccessorMock;
        ScopeProviderMock = scopeProviderMock;

        Service = new AIExtractionService(
            ProfileServiceMock.Object,
            factory,
            EventAggregatorMock.Object,
            contextAccessorMock.Object,
            scopeProviderMock.Object,
            contributors,
            new AIExperimentalFeatures(experimentalOptions.Object));
    }

    public AIExtractionService Service { get; }

    public AIProfile Profile { get; }

    public Mock<IAIProfileService> ProfileServiceMock { get; }

    /// <summary>The event aggregator mock the service publishes executing/executed notifications to.</summary>
    public Mock<IEventAggregator> EventAggregatorMock { get; }

    /// <summary>
    /// The runtime context a scope created via <see cref="ScopeProviderMock"/> hands back.
    /// </summary>
    public AIRuntimeContext Context { get; }

    public Mock<IAIRuntimeContextAccessor> ContextAccessorMock { get; }

    public Mock<IAIRuntimeContextScopeProvider> ScopeProviderMock { get; }

    private sealed class SingleSettingsExtractionCapability(IAIProvider provider, IAIExtractionClient inner)
        : AIExtractionCapabilityBase<FakeProviderSettings>(provider)
    {
        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override IAIExtractionClient CreateClient(FakeProviderSettings settings, string? modelId)
            => inner;
    }
}
