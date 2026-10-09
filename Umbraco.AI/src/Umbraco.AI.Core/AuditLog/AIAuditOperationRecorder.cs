using System.Diagnostics;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Observability;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Writes an audit log entry for every tracked AI call when auditing is enabled: the entry and its
/// parent link at the start, the ambient <see cref="AIAuditScope"/> while the call runs, and the end
/// status (completed, failed or blocked) at the end.
/// </summary>
internal sealed class AIAuditOperationRecorder : IAIOperationRecorder
{
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    public AIAuditOperationRecorder(
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
    {
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    public async ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken)
    {
        if (!_auditLogOptions.CurrentValue.Enabled || start.Identity is null)
        {
            return null;
        }

        var descriptor = start.Descriptor;
        var auditContext = AIAuditContext.FromUsageContext(start.Identity, descriptor.PromptData);

        // The parent is whichever call's scope is open around this one. This call's own scope is only
        // entered later, around the work (see Recording.EnterScope), so it can't be picked up here.
        var auditLog = _auditLogFactory.Create(auditContext, start.LogValues, parentId: AIAuditScope.Current?.AuditLogId);

        // Link the entry to its trace. The trace side (the audit ID tag on the call's span) is added by
        // AITraceOperationRecorder from this entry's scope.
        auditLog.TraceId = Activity.Current?.TraceId.ToString();

        await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);

        var auditPrompt = new AIAuditPrompt { Data = descriptor.PromptData, Capability = descriptor.Capability };
        return new Recording(_auditLogService, auditLog, auditPrompt);
    }

    private sealed class Recording(
        IAIAuditLogService auditLogService,
        AIAuditLog auditLog,
        AIAuditPrompt auditPrompt) : IAIOperationRecording
    {
        /// <summary>
        /// Makes this call's audit entry the parent of any AI call made while the scope is open.
        /// </summary>
        public IDisposable EnterScope() => AIAuditScope.Begin(auditLog.Id);

        // The end status is queued on CancellationToken.None so a cancelled call still gets one.
        public async ValueTask EndAsync(AIOperationOutcome outcome)
        {
            if (outcome.Succeeded)
            {
                var response = new AIAuditResponse { Data = outcome.ResponseData, Usage = outcome.Usage };
                await auditLogService.QueueCompleteAuditLogAsync(
                    auditLog, auditPrompt, response, CancellationToken.None);
            }
            else
            {
                await auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, outcome.Exception!, CancellationToken.None);
            }
        }
    }
}
