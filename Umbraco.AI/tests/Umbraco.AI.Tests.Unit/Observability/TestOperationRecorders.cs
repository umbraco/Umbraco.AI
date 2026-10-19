using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.Telemetry;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Tests.Unit.Observability;

/// <summary>
/// The tracker's default recorders, in their registered order, for tests that build an
/// <see cref="AIOperationTracker"/> by hand.
/// </summary>
internal static class TestOperationRecorders
{
    public static IAIOperationRecorder[] Default(
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory usageRecordFactory,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        IBackOfficeSecurityAccessor? securityAccessor = null) =>
    [
        new AIAuditOperationRecorder(auditLogService, auditLogFactory, auditLogOptions),
        new AITraceOperationRecorder(securityAccessor ?? Mock.Of<IBackOfficeSecurityAccessor>()),
        new AIAnalyticsOperationRecorder(
            usageRecordingService,
            usageRecordFactory,
            analyticsOptions,
            NullLogger<AIAnalyticsOperationRecorder>.Instance),
        new AITestUsageOperationRecorder(NullLogger<AITestUsageOperationRecorder>.Instance),
    ];
}
