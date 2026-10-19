using Umbraco.AI.Tests.Unit.Observability;
#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Tests the experimental image-generation tracking middleware

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.ImageGeneration;

/// <summary>
/// Tests for <see cref="AITrackingImageGenerationClient"/>, verified end-to-end through a real
/// <see cref="AIOperationTracker"/> wired with mocked audit/usage collaborators.
/// </summary>
public class AITrackingImageGenerationClientTests
{
    private readonly Mock<IAIAuditLogService> _auditLogServiceMock = new();
    private readonly AIOperationTracker _tracker;
    private readonly AIAuditLog _auditLog = new() { Id = Guid.NewGuid() };

    public AITrackingImageGenerationClientTests()
    {
        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, Guid.NewGuid());
        runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "test-profile");
        runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "openai");
        runtimeContext.SetValue(Constants.ContextKeys.ModelId, "gpt-image-1");
        var contextAccessor = new Mock<IAIRuntimeContextAccessor>();
        contextAccessor.Setup(x => x.Context).Returns(runtimeContext);

        var auditLogFactory = new Mock<IAIAuditLogFactory>();
        auditLogFactory
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(_auditLog);

        var auditOptions = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        auditOptions.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });
        var analyticsOptions = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        analyticsOptions.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });

        _tracker = new AIOperationTracker(
            contextAccessor.Object,
            TestOperationRecorders.Default(_auditLogServiceMock.Object, auditLogFactory.Object, auditOptions.Object, new Mock<IAIUsageRecordingService>().Object, new Mock<IAIUsageRecordFactory>().Object, analyticsOptions.Object),
            NullLogger<AIOperationTracker>.Instance);
    }

    [Fact]
    public async Task GenerateAsync_OnSuccess_QueuesCompleteAuditWithUsage()
    {
        // Arrange
        var usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 };
        var fake = new FakeImageGenerator(new ImageGenerationResponse([new DataContent(new byte[] { 1, 2, 3 }, "image/png")])
        {
            Usage = usage,
        });
        var generator = new AITrackingImageGenerationClient(fake, _tracker);

        // Act
        await generator.GenerateAsync(new ImageGenerationRequest("a cat"));

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.Is<AIAuditResponse?>(r =>
                r != null &&
                (string?)r.Data == "1 image(s)" &&
                r.Usage == usage),
            CancellationToken.None), Times.Once);
    }
}
