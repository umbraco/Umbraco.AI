#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

/// <summary>
/// DC-3 (AC2). One representative happy-path case, mirroring
/// <c>AITrackingSpeechToTextClientTests</c> — the same shared <see cref="AIOperationTracker"/>
/// backs every capability's tracking client, so this proves Decision plugs into it the same way
/// rather than re-testing every failure/cancellation branch <c>AITrackingSpeechToTextClientTests</c>
/// already covers for the tracker itself.
/// </summary>
public class AITrackingDecisionClientTests
{
    private readonly Mock<IAIRuntimeContextAccessor> _contextAccessorMock;
    private readonly Mock<IAIAuditLogService> _auditLogServiceMock;
    private readonly Mock<IAIAuditLogFactory> _auditLogFactoryMock;
    private readonly Mock<IAIUsageRecordingService> _usageRecordingServiceMock;
    private readonly Mock<IAIUsageRecordFactory> _usageRecordFactoryMock;
    private readonly Mock<IOptionsMonitor<AIAuditLogOptions>> _auditLogOptionsMock;
    private readonly Mock<IOptionsMonitor<AIAnalyticsOptions>> _analyticsOptionsMock;
    private readonly AIAuditLog _auditLog;

    public AITrackingDecisionClientTests()
    {
        _contextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        _auditLogServiceMock = new Mock<IAIAuditLogService>();
        _auditLogFactoryMock = new Mock<IAIAuditLogFactory>();
        _usageRecordingServiceMock = new Mock<IAIUsageRecordingService>();
        _usageRecordFactoryMock = new Mock<IAIUsageRecordFactory>();

        _auditLogOptionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        _auditLogOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions { Enabled = true });

        _analyticsOptionsMock = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        _analyticsOptionsMock.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions { Enabled = true });

        var runtimeContext = new AIRuntimeContext([]);
        runtimeContext.SetValue(Constants.ContextKeys.ProfileId, Guid.NewGuid());
        runtimeContext.SetValue(Constants.ContextKeys.ProfileAlias, "test-decision-profile");
        runtimeContext.SetValue(Constants.ContextKeys.ProviderId, "fake-decision-provider");
        runtimeContext.SetValue(Constants.ContextKeys.ModelId, "jev-test");
        _contextAccessorMock.Setup(x => x.Context).Returns(runtimeContext);

        _auditLog = new AIAuditLog { Id = Guid.NewGuid() };
        _auditLogFactoryMock
            .Setup(x => x.Create(It.IsAny<AIAuditContext>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<Guid?>()))
            .Returns(_auditLog);
        _auditLogServiceMock
            .Setup(x => x.QueueStartAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _auditLogServiceMock
            .Setup(x => x.QueueCompleteAuditLogAsync(It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
    }

    [Fact]
    public async Task AskAsync_OnSuccess_QueuesCompleteAudit()
    {
        // Arrange
        var fakeClient = new FakeDecisionClient(_ => AIDecisionResponse.ForBinary(true, 0.9));
        var tracker = new AIOperationTracker(
            _contextAccessorMock.Object,
            _auditLogServiceMock.Object,
            _auditLogFactoryMock.Object,
            _auditLogOptionsMock.Object,
            _usageRecordingServiceMock.Object,
            _usageRecordFactoryMock.Object,
            _analyticsOptionsMock.Object,
            NullLogger<AIOperationTracker>.Instance);
        var client = new AITrackingDecisionClient(fakeClient, tracker, _contextAccessorMock.Object);

        // Act
        await client.AskAsync(new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this spam?" });

        // Assert
        _auditLogServiceMock.Verify(x => x.QueueCompleteAuditLogAsync(
            _auditLog,
            It.IsAny<AIAuditPrompt?>(),
            It.IsAny<AIAuditResponse?>(),
            CancellationToken.None), Times.Once);
    }
}
