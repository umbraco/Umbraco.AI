using Umbraco.AI.Tests.Unit.Observability;
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// Shared arrange for the Extraction tracking specs, copied from DecisionTrackingAndChecksHarness: the real
// AIExtractionClientFactory with a real AIOperationTracker in its middleware. Only the audit/usage
// collaborators are mocked, so the specs can see what the tracker wrote.
//
// ASSUMPTION (builder confirms/adjusts): AITrackingExtractionMiddleware takes an IAIOperationTracker, like
// AITrackingDecisionMiddleware.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// The real extraction client pipeline (validating → scoped profile → tracking → error classifier →
/// <paramref name="innerClient"/>), with the tracker's audit and usage sinks mocked for observation.
/// </summary>
internal sealed class ExtractionTrackingAndChecksHarness
{
    private const string ProviderId = "fake-provider";
    private const string ModelId = "fake-model-1";

    private ExtractionTrackingAndChecksHarness(
        IAIExtractionClient client,
        Mock<IAIAuditLogService> auditLogServiceMock,
        Mock<IAIAuditLogFactory> auditLogFactoryMock,
        Mock<IAIUsageRecordingService> usageRecordingServiceMock,
        Mock<IAIUsageRecordFactory> usageRecordFactoryMock)
    {
        Client = client;
        AuditLogServiceMock = auditLogServiceMock;
        AuditLogFactoryMock = auditLogFactoryMock;
        UsageRecordingServiceMock = usageRecordingServiceMock;
        UsageRecordFactoryMock = usageRecordFactoryMock;
    }

    public IAIExtractionClient Client { get; }

    public Mock<IAIAuditLogService> AuditLogServiceMock { get; }

    /// <summary>Receives the <see cref="AIAuditContext"/> each audit entry is built from.</summary>
    public Mock<IAIAuditLogFactory> AuditLogFactoryMock { get; }

    public Mock<IAIUsageRecordingService> UsageRecordingServiceMock { get; }

    /// <summary>Receives the <see cref="AIUsageRecordContext"/>/<see cref="AIUsageRecordResult"/> each usage record is built from.</summary>
    public Mock<IAIUsageRecordFactory> UsageRecordFactoryMock { get; }

    public static async Task<ExtractionTrackingAndChecksHarness> CreateAsync(IAIExtractionClient innerClient)
    {
        var context = new AIRuntimeContext([]);
        var scope = new Mock<IAIRuntimeContextScope>();
        scope.Setup(x => x.Context).Returns(context);
        var contextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        contextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);
        var scopeProviderMock = new Mock<IAIRuntimeContextScopeProvider>();
        scopeProviderMock
            .Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()))
            .Returns(() =>
            {
                contextAccessorMock.Setup(x => x.Context).Returns(context);
                return scope.Object;
            });

        var auditLogServiceMock = new Mock<IAIAuditLogService>();
        auditLogServiceMock
            .Setup(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        auditLogServiceMock
            .Setup(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        auditLogServiceMock
            .Setup(x => x.QueueRecordAuditLogFailureAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var auditLogFactoryMock = new Mock<IAIAuditLogFactory>();
        auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(() => new AIAuditLog { Id = Guid.NewGuid() });

        var usageRecordingServiceMock = new Mock<IAIUsageRecordingService>();
        usageRecordingServiceMock
            .Setup(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var usageRecordFactoryMock = new Mock<IAIUsageRecordFactory>();
        usageRecordFactoryMock
            .Setup(x => x.Create(It.IsAny<AIUsageRecordContext>(), It.IsAny<AIUsageRecordResult>()))
            .Returns(() => new AIUsageRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Capability = AICapability.Extraction,
                ProfileId = Guid.NewGuid(),
                ProfileAlias = "test-extraction-profile",
                ProviderId = ProviderId,
                ModelId = ModelId,
                InputTokens = 0,
                OutputTokens = 0,
                TotalTokens = 0,
                DurationMs = 0,
                Status = AIUsageRecordStatus.Succeeded,
                CreatedAt = DateTime.UtcNow,
            });

        var auditLogOptionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });
        var analyticsOptionsMock = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        analyticsOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });

        var tracker = new AIOperationTracker(
            contextAccessorMock.Object,
            TestOperationRecorders.Default(auditLogServiceMock.Object, auditLogFactoryMock.Object, auditLogOptionsMock.Object, usageRecordingServiceMock.Object, usageRecordFactoryMock.Object, analyticsOptionsMock.Object),
            NullLogger<AIOperationTracker>.Instance);

        var middleware = new AIExtractionMiddlewareCollection(() => new IAIExtractionMiddleware[]
        {
            new AITrackingExtractionMiddleware(tracker),
        });

        var connectionId = Guid.NewGuid();
        var connectionSettings = new FakeProviderSettings { ApiKey = "test-key" };
        var connection = new AIConnectionBuilder()
            .WithId(connectionId).WithProviderId(ProviderId).WithSettings(connectionSettings).IsActive(true).Build();
        var profile = new AIProfileBuilder()
            .WithConnectionId(connectionId).WithModel(ProviderId, ModelId).WithCapability(AICapability.Extraction).Build();

        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var configuredCapability = new AIConfiguredExtractionCapability(
            new SingleSettingsExtractionCapability(provider, innerClient), connectionSettings);
        var configuredProviderMock = new Mock<IAIConfiguredProvider>();
        configuredProviderMock.Setup(x => x.Provider).Returns(provider);
        configuredProviderMock.Setup(x => x.GetCapability<IAIConfiguredExtractionCapability>()).Returns(configuredCapability);

        var connectionServiceMock = new Mock<IAIConnectionService>();
        connectionServiceMock.Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        connectionServiceMock.Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>())).ReturnsAsync(configuredProviderMock.Object);

        var factory = new AIExtractionClientFactory(
            connectionServiceMock.Object,
            middleware,
            contextAccessorMock.Object,
            scopeProviderMock.Object,
            new AIRuntimeContextContributorCollection(Enumerable.Empty<IAIRuntimeContextContributor>),
            new Mock<IAIEditableModelResolver>().Object);

        var client = await factory.CreateClientAsync(profile);
        return new ExtractionTrackingAndChecksHarness(
            client, auditLogServiceMock, auditLogFactoryMock, usageRecordingServiceMock, usageRecordFactoryMock);
    }

    private sealed class SingleSettingsExtractionCapability(IAIProvider provider, IAIExtractionClient inner)
        : AIExtractionCapabilityBase<FakeProviderSettings>(provider)
    {
        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);

        protected override IAIExtractionClient CreateClient(FakeProviderSettings settings, string? modelId) => inner;
    }
}
