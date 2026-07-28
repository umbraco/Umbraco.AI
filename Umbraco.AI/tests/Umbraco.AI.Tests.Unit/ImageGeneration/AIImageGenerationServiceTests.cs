#pragma warning disable MEAI001 // Image generation types are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Tests the experimental image-generation service

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Tests.Unit.ImageGeneration;

public class AIImageGenerationServiceTests
{
    private readonly Mock<IAIImageGeneratorFactory> _factoryMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIConnectionService> _connectionServiceMock = new();
    private readonly Mock<IEventAggregator> _eventAggregatorMock = new();
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock = new();
    private readonly Mock<IAIRuntimeContextScopeProvider> _scopeProviderMock = new();
    private readonly Mock<IAIAuditLogService> _auditLogServiceMock = new();
    private readonly Mock<IAIAuditLogFactory> _auditLogFactoryMock = new();
    private readonly Mock<IAIUsageRecordingService> _usageRecordingServiceMock = new();
    private readonly Mock<IAIUsageRecordFactory> _usageRecordFactoryMock = new();
    private readonly AIImageGenerationService _service;

    public AIImageGenerationServiceTests()
    {
        _eventAggregatorMock
            .Setup(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var runtimeContext = new AIRuntimeContext([]);
        _contextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);

        var mockScope = new Mock<IAIRuntimeContextScope>();
        mockScope.Setup(s => s.Context).Returns(runtimeContext);
        _scopeProviderMock
            .Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>()))
            .Returns(() =>
            {
                _contextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);
                return mockScope.Object;
            });

        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(new AIAuditLog());

        var analyticsOptions = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        analyticsOptions.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });
        var auditOptions = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        auditOptions.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });

        var contributors = new AIRuntimeContextContributorCollection(() => []);

        // The tracked helper records via the shared tracker (same component the middleware uses),
        // so the usage/audit assertions exercise it through a real tracker built from these mocks.
        var tracker = new AIOperationTracker(
            _contextAccessorMock.Object,
            _auditLogServiceMock.Object,
            _auditLogFactoryMock.Object,
            auditOptions.Object,
            _usageRecordingServiceMock.Object,
            _usageRecordFactoryMock.Object,
            analyticsOptions.Object,
            NullLogger<AIOperationTracker>.Instance);

        _service = new AIImageGenerationService(
            _factoryMock.Object,
            _profileServiceMock.Object,
            new Mock<IAIGuardrailService>().Object,
            _connectionServiceMock.Object,
            _eventAggregatorMock.Object,
            _contextAccessorMock.Object,
            _scopeProviderMock.Object,
            contributors,
            tracker);
    }

    private AIProfile SetupDefaultImageProfile(IImageGenerator generator)
    {
        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.ImageGeneration)
            .WithModel("fake-provider", "gpt-image-1")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.ImageGeneration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _factoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generator);

        return profile;
    }

    [Fact]
    public async Task GenerateImagesAsync_PassesPromptToGenerator()
    {
        var generator = new FakeImageGenerator();
        SetupDefaultImageProfile(generator);

        var response = await _service.GenerateImagesAsync(b => b.WithAlias("test"), "a cat");

        response.ShouldNotBeNull();
        generator.ReceivedRequests.Count.ShouldBe(1);
        generator.ReceivedRequests[0].Prompt.ShouldBe("a cat");
        generator.ReceivedRequests[0].OriginalImages.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateImagesAsync_DoesNotInventProviderHints()
    {
        // Quality and style used to travel from here as additional properties, which the OpenAI adapter
        // ignored — so they did nothing at all. They are now provider-declared capability settings, applied
        // by the provider itself. This holds the core side of that move: nothing here fabricates a hint,
        // so there is only ever one place those values come from.
        var generator = new FakeImageGenerator();
        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.ImageGeneration)
            .WithModel("fake-provider", "dall-e-3")
            .WithSettings(new AIImageGenerationProfileSettings { Size = "1024x1024", MediaType = "image/png" })
            .Build();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.ImageGeneration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _factoryMock
            .Setup(x => x.CreateGeneratorAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generator);

        await _service.GenerateImagesAsync(b => b.WithAlias("hints"), "a cat");

        var options = generator.ReceivedOptions.ShouldHaveSingleItem();
        // The first-class settings still flow, because M.E.AI models them.
        options!.ImageSize.ShouldNotBeNull();
        options.MediaType.ShouldBe("image/png");
        options.AdditionalProperties?.ContainsKey("quality").ShouldNotBe(true);
        options.AdditionalProperties?.ContainsKey("style").ShouldNotBe(true);
    }

    [Fact]
    public async Task GenerateImagesAsync_WithOriginalImages_FlowsThroughToGenerator()
    {
        var generator = new FakeImageGenerator();
        SetupDefaultImageProfile(generator);

        var originalImages = new List<AIContent> { new DataContent(new byte[] { 9, 9 }, "image/png") };

        await _service.GenerateImagesAsync(b => b.WithAlias("edit"), "make it blue", originalImages);

        generator.ReceivedRequests.Count.ShouldBe(1);
        generator.ReceivedRequests[0].OriginalImages.ShouldNotBeNull();
        generator.ReceivedRequests[0].OriginalImages!.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GenerateImagesAsync_PublishesNotifications()
    {
        SetupDefaultImageProfile(new FakeImageGenerator());

        await _service.GenerateImagesAsync(b => b.WithAlias("notify"), "a dog");

        _eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIImageGenerationExecutingNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIImageGenerationExecutedNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateImagesAsync_WithNonImageProfile_Throws()
    {
        var chatProfile = new AIProfileBuilder()
            .WithCapability(AICapability.Chat)
            .WithName("Chat Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.ImageGeneration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfile);

        var act = () => _service.GenerateImagesAsync(b => b.WithAlias("bad"), "prompt");

        var ex = await Should.ThrowAsync<InvalidOperationException>(act);
        ex.Message.ShouldContain("does not support image-generation capability");
    }

    [Fact]
    public async Task CreateImageGeneratorAsync_ReturnsGenerator_ResolvingProviderClientViaGetService()
    {
        var surrogate = new object();
        var generator = new FakeImageGenerator().RegisterService(typeof(AIImageGenerationServiceTests), surrogate);
        SetupDefaultImageProfile(generator);

        var result = await _service.CreateImageGeneratorAsync(b => b.WithAlias("escape"));

        // The escape hatch: GetService forwards through the inline scoped wrapper to the provider generator.
        result.GetService(typeof(AIImageGenerationServiceTests)).ShouldBeSameAs(surrogate);
    }

    [Fact]
    public async Task InvokeWithTrackingAsync_OnSuccess_RecordsUsageAndAudit()
    {
        var generator = new FakeImageGenerator();
        SetupDefaultImageProfile(generator);

        var result = await _service.InvokeWithTrackingAsync<string>(
            b => b.WithAlias("tracked"),
            (gen, ct) => Task.FromResult(new AITrackedImageResult<string>
            {
                Result = "done",
                Usage = new UsageDetails { TotalTokenCount = 42 },
                ImageCount = 1,
            }));

        result.Result.ShouldBe("done");
        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()), Times.Once);
        _usageRecordingServiceMock.Verify(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeWithTrackingAsync_OnFailure_RecordsAuditFailure()
    {
        var generator = new FakeImageGenerator();
        SetupDefaultImageProfile(generator);

        var act = () => _service.InvokeWithTrackingAsync<string>(
            b => b.WithAlias("tracked-fail"),
            (gen, ct) => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(act);

        _auditLogServiceMock.Verify(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogServiceMock.Verify(x => x.QueueRecordAuditLogFailureAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSupportedModelsAsync_ReturnsModelsWithMetadataAndModelId()
    {
        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.ImageGeneration)
            .WithModel("fake-provider", "gpt-image-1")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.ImageGeneration, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var configuredCapability = new Mock<IAIConfiguredImageGeneratorCapability>();
        configuredCapability
            .Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIModelDescriptor>
            {
                new(new AIModelRef("fake-provider", "gpt-image-1"), "GPT Image 1",
                    new Dictionary<string, string> { [AIModelMetadataKeys.ImageSupportedSizes] = "1024x1024" }),
            });

        var configuredProvider = new Mock<IAIConfiguredProvider>();
        configuredProvider
            .Setup(x => x.GetCapability<IAIConfiguredImageGeneratorCapability>())
            .Returns(configuredCapability.Object);

        _connectionServiceMock
            .Setup(x => x.GetConfiguredProviderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredProvider.Object);

        // No WithAlias() — a metadata lookup only needs a profile, not an alias. This would throw the
        // "alias is required" validation the generation paths enforce if the query still validated.
        var result = await _service.GetSupportedModelsAsync(_ => { });

        result.ModelId.ShouldBe("gpt-image-1");
        result.Models.Count.ShouldBe(1);
        // Deliberately the literal rather than the constant: this is the key that travels to the backoffice,
        // so a change to the constant's value should fail here rather than silently rename the contract.
        result.Models[0].Metadata.ContainsKey("image.supportedSizes").ShouldBeTrue();
    }
}
