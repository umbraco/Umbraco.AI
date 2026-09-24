#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// Locks the critical wrapping order <see cref="AIDecisionClientFactory.CreateClientAsync"/> builds —
/// innermost provider client → <see cref="AIErrorClassifyingDecisionClient"/> → middleware (e.g.
/// tracking) → <see cref="ScopedProfileDecisionClient"/> → <see cref="ValidatingDecisionClient"/>
/// outermost — so a future reorder is caught here rather than by hand-tracing the factory again.
/// </summary>
/// <remarks>
/// Exercises the real <see cref="AIDecisionClientFactory"/> end to end, the same way
/// <c>CapabilitySettingsRoundTripTests</c> does for its sibling factories: mocking the factory itself,
/// or the client it returns, would leave a future reorder of its wrapping undetected.
/// </remarks>
public class AIDecisionClientFactoryTests
{
    private const string ProviderId = "fake-provider";
    private const string ModelId = "fake-model-1";

    private readonly Mock<IAIConnectionService> _connectionServiceMock = new();
    private readonly Mock<IAIEditableModelResolver> _modelResolverMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock = new();
    private readonly Mock<IAIRuntimeContextScopeProvider> _scopeProviderMock = new();
    private readonly AIRuntimeContextContributorCollection _contributors =
        new(Enumerable.Empty<IAIRuntimeContextContributor>);

    public AIDecisionClientFactoryTests()
    {
        // ScopedProfileDecisionClient opens a runtime-context scope per call when none exists already;
        // give it a real context so it doesn't fault before the request under test runs.
        var context = new AIRuntimeContext([]);
        var scope = new Mock<IAIRuntimeContextScope>();
        scope.Setup(x => x.Context).Returns(context);

        _contextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);
        _scopeProviderMock
            .Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()))
            .Returns(() =>
            {
                _contextAccessorMock.Setup(x => x.Context).Returns(context);
                return scope.Object;
            });
    }

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_ThrowsArgumentExceptionNotProviderException()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var (factory, profile) = ArrangeFactory(throwingClient);
        var client = await factory.CreateClientAsync(profile);

        // Act
        var act = () => client.AskAsync(InvalidQuestion());

        // Assert — rejected as the caller's mistake, not misreported as a provider failure.
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_NeverInvokesProviderClient()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var (factory, profile) = ArrangeFactory(throwingClient);
        var client = await factory.CreateClientAsync(profile);

        // Act
        await Should.ThrowAsync<ArgumentException>(() => client.AskAsync(InvalidQuestion()));

        // Assert — ValidatingDecisionClient (outermost) rejected the question before it ever reached
        // the provider's client.
        throwingClient.ReceivedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task AskAsync_WithInvalidQuestion_NeverInvokesTracker()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "The provider client must never be reached for a caller error."));
        var trackerMock = new Mock<IAIOperationTracker>();
        var middleware = new AIDecisionMiddlewareCollection(() => new IAIDecisionMiddleware[]
        {
            new AITrackingDecisionMiddleware(trackerMock.Object, _contextAccessorMock.Object),
        });
        var (factory, profile) = ArrangeFactory(throwingClient, middleware);
        var client = await factory.CreateClientAsync(profile);

        // Act
        await Should.ThrowAsync<ArgumentException>(() => client.AskAsync(InvalidQuestion()));

        // Assert — tracking middleware sits inside ValidatingDecisionClient, so a caller error must
        // never reach it; a caller mistake must not be recorded as a tracked/audited operation.
        trackerMock.Verify(
            x => x.TrackAsync<AIDecisionResponse>(
                It.IsAny<AIOperationDescriptor>(),
                It.IsAny<Func<CancellationToken, Task<AITrackedOperationResult<AIDecisionResponse>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AskAsync_WithGenuineProviderFailure_ThrowsAIProviderException()
    {
        // Arrange
        var throwingClient = new FakeDecisionClient(_ => throw new InvalidOperationException(
            "Simulated provider SDK failure."));
        var (factory, profile) = ArrangeFactory(throwingClient);
        var client = await factory.CreateClientAsync(profile);

        // Act
        var act = () => client.AskAsync(ValidQuestion());

        // Assert — a real SDK failure (not a caller error) still comes out classified, proving
        // AIErrorClassifyingDecisionClient still does its job for genuine provider failures.
        await Should.ThrowAsync<AIProviderException>(act);
    }

    /// <summary>
    /// A provider that answers the wrong response shape must be rejected as the caller sees it — this
    /// pins the observable contract from <see cref="AIErrorClassifyingDecisionClient"/>'s remarks: the
    /// caller still gets an <see cref="AIProviderException"/>, not the provider's mismatched response nor
    /// an <see cref="InvalidCastException"/> leaking an implementation detail.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithMismatchedResponseType_ThrowsAIProviderException()
    {
        // Arrange — a binary question answered with a choice response.
        var mismatchedClient = new FakeDecisionClient(_ => new AIChoiceDecisionResponse { Choice = "a", ChoiceConfidence = 0.8 });
        var (factory, profile) = ArrangeFactory(mismatchedClient);
        var client = await factory.CreateClientAsync(profile);

        // Act
        var act = () => client.AskAsync(ValidQuestion());

        // Assert
        await Should.ThrowAsync<AIProviderException>(act);
    }

    /// <summary>
    /// The mismatch check lives in <see cref="AIErrorClassifyingDecisionClient"/>, which
    /// <see cref="AIDecisionClientFactory"/> wraps *inside* the tracking middleware (see the wrapping
    /// order this test class documents at the top). That's what makes the tracker/audit see the
    /// mismatch as a failed operation rather than a successful one whose result the caller is then told
    /// is wrong.
    /// </summary>
    [Fact]
    public async Task AskAsync_WithMismatchedResponseType_RecordsAuditFailure()
    {
        // Arrange
        var mismatchedClient = new FakeDecisionClient(_ => new AIChoiceDecisionResponse { Choice = "a", ChoiceConfidence = 0.8 });
        var (client, auditLogServiceMock) = await ArrangeFactoryWithTrackingAsync(mismatchedClient);

        // Act
        await Should.ThrowAsync<AIProviderException>(() => client.AskAsync(ValidQuestion()));

        // Assert
        auditLogServiceMock.Verify(
            x => x.QueueRecordAuditLogFailureAsync(
                It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AskAsync_WithMismatchedResponseType_NeverRecordsAuditSuccess()
    {
        // Arrange
        var mismatchedClient = new FakeDecisionClient(_ => new AIChoiceDecisionResponse { Choice = "a", ChoiceConfidence = 0.8 });
        var (client, auditLogServiceMock) = await ArrangeFactoryWithTrackingAsync(mismatchedClient);

        // Act
        await Should.ThrowAsync<AIProviderException>(() => client.AskAsync(ValidQuestion()));

        // Assert
        auditLogServiceMock.Verify(
            x => x.QueueCompleteAuditLogAsync(
                It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AIDecisionQuestion InvalidQuestion() => new AIChoiceDecisionQuestion
    {
        Instructions = "pick one",
        Options = [new AIDecisionOption("only-one")],
    };

    private static AIDecisionQuestion ValidQuestion() => new AIBinaryDecisionQuestion
    {
        Instructions = "is this spam?",
    };

    /// <summary>
    /// Wires the connection service and a real <see cref="AIConfiguredDecisionCapability"/>/
    /// <see cref="AIDecisionCapabilityBase{TSettings}"/> so the real factory reaches
    /// <paramref name="innerClient"/> — the earliest point at which each test's fake takes over.
    /// </summary>
    private (AIDecisionClientFactory Factory, AIProfile Profile) ArrangeFactory(
        IAIDecisionClient innerClient,
        AIDecisionMiddlewareCollection? middleware = null)
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
            .WithCapability(AICapability.Decision)
            .Build();

        var provider = new FakeAIProvider(ProviderId, "Fake Provider");
        var capability = new SingleSettingsDecisionCapability(provider, innerClient);
        var configuredCapability = new AIConfiguredDecisionCapability(capability, connectionSettings);

        var configuredProviderMock = new Mock<IAIConfiguredProvider>();
        configuredProviderMock.Setup(x => x.Provider).Returns(provider);
        configuredProviderMock.Setup(x => x.GetCapability<IAIConfiguredDecisionCapability>()).Returns(configuredCapability);

        _connectionServiceMock
            .Setup(x => x.GetConnectionAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProviderMock.Object);

        var factory = new AIDecisionClientFactory(
            _connectionServiceMock.Object,
            middleware ?? new AIDecisionMiddlewareCollection(Enumerable.Empty<IAIDecisionMiddleware>),
            _contextAccessorMock.Object,
            _scopeProviderMock.Object,
            _contributors,
            _modelResolverMock.Object);

        return (factory, profile);
    }

    /// <summary>
    /// Builds the real factory with a real <see cref="AIOperationTracker"/> (only its audit-log
    /// collaborator is mocked, so calls to it can be observed) wired into the middleware pipeline —
    /// mirrors <c>AITrackingDecisionClientTests.CreateTracker</c> — and returns the client it produces.
    /// </summary>
    private async Task<(IAIDecisionClient Client, Mock<IAIAuditLogService> AuditLogServiceMock)> ArrangeFactoryWithTrackingAsync(
        IAIDecisionClient innerClient)
    {
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
            .Returns(new AIAuditLog { Id = Guid.NewGuid() });

        var auditLogOptionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });

        var analyticsOptionsMock = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        analyticsOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = false });

        var tracker = new AIOperationTracker(
            _contextAccessorMock.Object,
            auditLogServiceMock.Object,
            auditLogFactoryMock.Object,
            auditLogOptionsMock.Object,
            Mock.Of<IAIUsageRecordingService>(),
            Mock.Of<IAIUsageRecordFactory>(),
            analyticsOptionsMock.Object,
            NullLogger<AIOperationTracker>.Instance);

        var middleware = new AIDecisionMiddlewareCollection(() => new IAIDecisionMiddleware[]
        {
            new AITrackingDecisionMiddleware(tracker, _contextAccessorMock.Object),
        });

        var (factory, profile) = ArrangeFactory(innerClient, middleware);
        var client = await factory.CreateClientAsync(profile);

        return (client, auditLogServiceMock);
    }

    /// <summary>The minimal real <see cref="IAIDecisionCapability"/> a provider package would define,
    /// with no provider-declared capability settings — this test is about wrapping order, not the
    /// capability-settings round trip <c>CapabilitySettingsRoundTripTests</c> already covers.</summary>
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
