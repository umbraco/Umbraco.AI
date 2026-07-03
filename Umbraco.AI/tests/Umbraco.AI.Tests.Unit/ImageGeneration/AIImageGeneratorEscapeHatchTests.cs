#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Tests the experimental image-generation escape hatch

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.ImageGeneration;

/// <summary>
/// Verifies the masked-outpainting escape hatch: GetService must forward through the entire
/// scoped + middleware stack so a consumer can resolve the provider-native clients.
/// </summary>
public class AIImageGeneratorEscapeHatchTests
{
    private sealed class FakeImageClientSurrogate;
    private sealed class FakeOpenAIClientSurrogate;

    [Fact]
    public void GetService_ResolvesProviderNativeClients_ThroughFullStack()
    {
        // Arrange — an innermost provider generator exposing both the bound image client and the un-bound
        // provider client surrogates (as the OpenAI decorator + adapter would).
        var imageClientSurrogate = new FakeImageClientSurrogate();
        var openAiClientSurrogate = new FakeOpenAIClientSurrogate();

        var fake = new FakeImageGenerator()
            .RegisterService(typeof(FakeImageClientSurrogate), imageClientSurrogate)
            .RegisterService(typeof(FakeOpenAIClientSurrogate), openAiClientSurrogate);

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.ImageGeneration)
            .WithModel("fake-provider", "gpt-image-1")
            .Build();

        var contextAccessor = new Mock<IAIRuntimeContextAccessor>();
        contextAccessor.Setup(x => x.Context).Returns(new AIRuntimeContext([]));
        var scopeProvider = new Mock<IAIRuntimeContextScopeProvider>();
        var contributors = new AIRuntimeContextContributorCollection(() => []);

        // The shared tracker drives usage + audit; GetService never invokes GenerateAsync, so its
        // dependencies just need to be present.
        var tracker = new AIOperationTracker(
            contextAccessor.Object,
            new Mock<IAIAuditLogService>().Object,
            new Mock<IAIAuditLogFactory>().Object,
            Mock.Of<IOptionsMonitor<AIAuditLogOptions>>(),
            new Mock<IAIUsageRecordingService>().Object,
            new Mock<IAIUsageRecordFactory>().Object,
            Mock.Of<IOptionsMonitor<AIAnalyticsOptions>>(),
            NullLogger<AIOperationTracker>.Instance);

        // Build the full pipeline exactly as AIImageGeneratorFactory does.
        IImageGenerator generator = fake;
        generator = new AIErrorClassifyingImageGenerator(generator, new Mock<IAIProvider>().Object);
        generator = new AITrackingImageGenerationClient(generator, tracker);
        generator = new ScopedProfileImageGenerator(generator, profile, contextAccessor.Object, scopeProvider.Object, contributors);

        // Act / Assert — both provider-native clients resolve through every wrapper.
        generator.GetService(typeof(FakeImageClientSurrogate)).ShouldBeSameAs(imageClientSurrogate);
        generator.GetService(typeof(FakeOpenAIClientSurrogate)).ShouldBeSameAs(openAiClientSurrogate);
    }
}
