using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // image types are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Internal plumbing for the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Single source of truth for recording usage analytics and audit entries around an image-generation
/// operation.
/// </summary>
/// <remarks>
/// <para>
/// Both entry points share this so the audit/usage orchestration is implemented once:
/// </para>
/// <list type="bullet">
///   <item>The tracking middleware (<see cref="AITrackingImageGenerationClient"/>) wraps the normal
///   <c>GenerateAsync</c> pipeline call.</item>
///   <item><see cref="IAIImageGenerationService.InvokeWithTrackingAsync{TResult}"/> wraps a raw
///   provider-native (escape-hatch) call that would otherwise bypass the middleware.</item>
/// </list>
/// <para>
/// All metadata is read from the ambient <see cref="AIRuntimeContext"/> (profile/model/provider/feature),
/// so the recording is identical regardless of which entry point invoked it. Audit logging and usage
/// analytics are independently gated by their respective <c>Enabled</c> options.
/// </para>
/// </remarks>
internal sealed class AIImageGenerationTracker
{
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _usageRecordFactory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _analyticsOptions;
    private readonly ILogger<AIImageGenerationTracker> _logger;

    public AIImageGenerationTracker(
        IAIRuntimeContextAccessor contextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory usageRecordFactory,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        ILogger<AIImageGenerationTracker> logger)
    {
        _contextAccessor = contextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
        _usageRecordingService = usageRecordingService;
        _usageRecordFactory = usageRecordFactory;
        _analyticsOptions = analyticsOptions;
        _logger = logger;
    }

    /// <summary>
    /// Runs <paramref name="operation"/>, recording an audit start/complete (or failure) and a usage
    /// record around it, reading all dimensions from the current runtime context.
    /// </summary>
    /// <typeparam name="TResult">The caller-defined operation result.</typeparam>
    /// <param name="promptData">Prompt/input descriptor captured for the audit entry.</param>
    /// <param name="operation">The operation to run; reports back usage and image count for recording.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AITrackedImageResult<TResult>> TrackAsync<TResult>(
        object? promptData,
        Func<CancellationToken, Task<AITrackedImageResult<TResult>>> operation,
        CancellationToken cancellationToken)
    {
        var (auditScope, auditLog) = await StartAuditLogAsync(promptData, cancellationToken);
        var auditPrompt = auditLog is not null
            ? new AIAuditPrompt { Data = promptData, Capability = AICapability.ImageGeneration }
            : null;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(cancellationToken);
            stopwatch.Stop();

            // Use CancellationToken.None so the status update is always persisted, even if the
            // original request was cancelled (e.g. client disconnected).
            if (auditLog is not null)
            {
                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    auditPrompt,
                    new AIAuditResponse { Data = $"{result.ImageCount ?? 0} image(s)" },
                    CancellationToken.None);
            }

            // Fire-and-forget so recording never delays the response.
            _ = RecordUsageAsync(result.Usage, stopwatch.ElapsedMilliseconds, succeeded: true, errorMessage: null, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (auditLog is not null)
            {
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, ex, CancellationToken.None);
            }

            _ = RecordUsageAsync(usage: null, stopwatch.ElapsedMilliseconds, succeeded: false, errorMessage: ex.Message, cancellationToken);

            throw;
        }
        finally
        {
            auditScope?.Dispose();
        }
    }

    private async Task<(AIAuditScope? Scope, AIAuditLog? AuditLog)> StartAuditLogAsync(
        object? promptData,
        CancellationToken cancellationToken)
    {
        if (!_auditLogOptions.CurrentValue.Enabled || _contextAccessor.Context is null)
        {
            return (null, null);
        }

        var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
            AICapability.ImageGeneration,
            _contextAccessor.Context,
            promptData);

        var auditLog = _auditLogFactory.Create(
            auditLogContext,
            metadata: null,
            parentId: AIAuditScope.Current?.AuditLogId);

        var auditScope = AIAuditScope.Begin(auditLog.Id);

        // Capture TraceId from the ambient Activity (created by the OpenTelemetry middleware).
        auditLog.TraceId = Activity.Current?.TraceId.ToString();

        await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);

        return (auditScope, auditLog);
    }

    private async Task RecordUsageAsync(
        UsageDetails? usage,
        long durationMs,
        bool succeeded,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_analyticsOptions.CurrentValue.Enabled || _contextAccessor.Context is null)
            {
                return;
            }

            var usageContext = AIUsageContext.ExtractFromRuntimeContext(AICapability.ImageGeneration, _contextAccessor.Context);
            var recordContext = AIUsageRecordContext.FromUsageContext(usageContext);

            var result = new AIUsageRecordResult
            {
                Usage = usage,
                DurationMs = durationMs,
                Succeeded = succeeded,
                ErrorMessage = errorMessage,
            };

            var record = _usageRecordFactory.Create(recordContext, result);
            await _usageRecordingService.QueueRecordUsageAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            // Recording failures must not break the main operation.
            _logger.LogError(ex, "Failed to record AI usage for image generation");
        }
    }
}
