using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// A tracking scope for a single AI operation. Created by <see cref="AIOperationTracker.BeginAsync"/>.
/// Completing or failing the scope queues the audit status (awaited, on <see cref="CancellationToken.None"/>)
/// and fire-and-forgets the usage record. Dispose ends the ambient <see cref="AIAuditScope"/>.
/// </summary>
internal sealed class AIOperationScope : IDisposable
{
    private readonly AIOperationTracker _tracker;
    private readonly AIOperationDescriptor _descriptor;
    private readonly AIAuditScope? _auditScope;
    private readonly AIAuditLog? _auditLog;
    private readonly AIAuditPrompt? _auditPrompt;
    private readonly Stopwatch _stopwatch;
    private readonly CancellationToken _cancellationToken;

    internal AIOperationScope(
        AIOperationTracker tracker,
        AIOperationDescriptor descriptor,
        AIAuditScope? auditScope,
        AIAuditLog? auditLog,
        AIAuditPrompt? auditPrompt,
        CancellationToken cancellationToken)
    {
        _tracker = tracker;
        _descriptor = descriptor;
        _auditScope = auditScope;
        _auditLog = auditLog;
        _auditPrompt = auditPrompt;
        _cancellationToken = cancellationToken;
        _stopwatch = Stopwatch.StartNew();
    }

    public async Task CompleteAsync(UsageDetails? usage, AIAuditResponse? auditResponse)
    {
        _stopwatch.Stop();

        if (_auditLog is not null)
        {
            await _tracker.AuditLogService.QueueCompleteAuditLogAsync(
                _auditLog, _auditPrompt, auditResponse, CancellationToken.None);
        }

        _ = _tracker.RecordUsageAsync(
            _descriptor, usage, _stopwatch.ElapsedMilliseconds, succeeded: true, errorMessage: null, _cancellationToken);
    }

    public async Task FailAsync(Exception exception)
    {
        _stopwatch.Stop();

        if (_auditLog is not null)
        {
            await _tracker.AuditLogService.QueueRecordAuditLogFailureAsync(
                _auditLog, _auditPrompt, exception, CancellationToken.None);
        }

        _ = _tracker.RecordUsageAsync(
            _descriptor, usage: null, _stopwatch.ElapsedMilliseconds, succeeded: false, errorMessage: exception.Message, _cancellationToken);
    }

    public void Dispose() => _auditScope?.Dispose();
}
