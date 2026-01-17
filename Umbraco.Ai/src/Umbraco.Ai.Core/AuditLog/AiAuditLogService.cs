using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.TaskQueue;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Service implementation for AI governance tracing operations.
/// </summary>
internal sealed class AiAuditLogService : IAiAuditLogService
{
    private readonly IAiAuditLogRepository _auditLogRepository;
    private readonly IOptionsMonitor<AiAuditLogOptions> _options;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<AiAuditLogService> _logger;

    public AiAuditLogService(
        IAiAuditLogRepository auditLogRepository,
        IOptionsMonitor<AiAuditLogOptions> options,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILoggerFactory loggerFactory)
    {
        _auditLogRepository = auditLogRepository;
        _options = options;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = loggerFactory.CreateLogger<AiAuditLogService>();
    }

    /// <inheritdoc />
    public async Task<AiAuditLog> StartAuditLogAsync(AiAuditLog auditLog,
        Guid? parentId = null,
        CancellationToken ct = default)
    {
        // The AiAuditLog.Create factory method should have already set most properties.
        // This method just handles parent ID resolution and persists to the database.

        // Set parent ID from explicit parameter or auto-detect from ambient scope
        var resolvedParentId = parentId ?? AiAuditScope.Current?.AuditLogId;
        if (resolvedParentId.HasValue && auditLog.ParentAuditLogId != resolvedParentId.Value)
        {
            // ParentAuditLogId is init-only, so we need to recreate the instance
            // This is expected to be rare - only when the caller didn't provide parentId to Create
            // but we detected it from AuditScope
            _logger.LogDebug(
                "Overriding parent audit log ID from {OldParentId} to {NewParentId} for audit {AuditLogId}",
                auditLog.ParentAuditLogId, resolvedParentId, auditLog.Id);

            auditLog.ParentAuditLogId = resolvedParentId;
        }

        // Ensure status is set to Running
        if (auditLog.Status != AiAuditLogStatus.Running)
        {
            auditLog.Status = AiAuditLogStatus.Running;
        }

        await _auditLogRepository.SaveAsync(auditLog, ct);

        _logger.LogDebug(
            "Started audit-log {AuditLogId} for {Capability} operation by user {UserId} (Parent: {ParentId})",
            auditLog.Id, auditLog.Capability, auditLog.UserId, auditLog.ParentAuditLogId);

        return auditLog;
    }

    /// <inheritdoc />
    public async Task CompleteAuditLogAsync(
        AiAuditLog audit,
        AiAuditResponse? response,
        CancellationToken ct = default)
    {

        audit.EndTime = DateTime.UtcNow;
        audit.Status = AiAuditLogStatus.Succeeded;
        
        if (response?.Usage is not null)
        {
            audit.InputTokens = response.Usage.InputTokenCount.HasValue
                ? (int?)response.Usage.InputTokenCount.Value
                : null;
            audit.OutputTokens = response.Usage.OutputTokenCount.HasValue
                ? (int?)response.Usage.OutputTokenCount.Value
                : null;
            audit.TotalTokens = response.Usage.TotalTokenCount.HasValue
                ? (int?)response.Usage.TotalTokenCount.Value
                : null;
        }
        
        if (_options.CurrentValue.PersistResponses && response?.Data != null && !string.IsNullOrEmpty(response.Data.ToString()))
        {
            audit.ResponseSnapshot = ApplyRedaction(response.Data.ToString());
        }

        await _auditLogRepository.SaveAsync(audit, ct);

        _logger.LogDebug(
            "Completed audit-log {AuditLogId} with status {Status} (Duration: {Duration}ms, Tokens: {TotalTokens})",
            audit.Id, AiAuditLogStatus.Succeeded, audit.Duration?.TotalMilliseconds, audit.TotalTokens);
    }

    /// <inheritdoc />
    public async Task RecordAuditLogFailureAsync(
        AiAuditLog audit,
        Exception exception,
        CancellationToken ct = default)
    {

        audit.EndTime = DateTime.UtcNow;
        audit.Status = AiAuditLogStatus.Failed;
        audit.ErrorMessage = exception.Message;
        audit.ErrorCategory = CategorizeError(exception);

        await _auditLogRepository.SaveAsync(audit, ct);

        if (_options.CurrentValue.PersistFailureDetails)
        {
            _logger.LogError(exception,
                "AuditLog {AuditLogId} failed with error: {ErrorMessage} (Duration: {Duration}ms)",
                audit.Id, exception.Message, audit.Duration?.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "AuditLog {AuditLogId} failed with error: {ErrorMessage} (Duration: {Duration}ms)",
                audit.Id, exception.Message, audit.Duration?.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public async ValueTask QueueStartAuditLogAsync(
        AiAuditLog auditLog,
        Guid? parentId = null,
        CancellationToken ct = default)
    {
        // IMPORTANT: Resolve parent ID from ambient scope NOW, before queuing,
        // because AuditScope.Current won't be available in the background worker context
        var resolvedParentId = parentId ?? AiAuditScope.Current?.AuditLogId;
        if (resolvedParentId.HasValue && auditLog.ParentAuditLogId != resolvedParentId.Value)
        {
            auditLog.ParentAuditLogId = resolvedParentId.Value;
        }

        // Ensure status is set to Running
        if (auditLog.Status != AiAuditLogStatus.Running)
        {
            auditLog.Status = AiAuditLogStatus.Running;
        }

        // Queue the persistence operation (all business logic already resolved above)
        var workItem = new BackgroundWorkItem(
            Name: "StartAuditLog",
            CorrelationId: auditLog.Id.ToString(),
            RunAsync: async (sp, token) =>
            {
                var repository = sp.GetRequiredService<IAiAuditLogRepository>();
                await repository.SaveAsync(auditLog, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug("Queued StartAuditLog for audit {AuditLogId} (Parent: {ParentId})",
            auditLog.Id, auditLog.ParentAuditLogId);
    }

    /// <inheritdoc />
    public async ValueTask QueueCompleteAuditLogAsync(
        AiAuditLog audit,
        AiAuditResponse? response,
        CancellationToken ct = default)
    {
        // Do all the business logic NOW, before queuing
        audit.EndTime = DateTime.UtcNow;
        audit.Status = AiAuditLogStatus.Succeeded;

        if (response?.Usage is not null)
        {
            audit.InputTokens = response.Usage.InputTokenCount.HasValue
                ? (int?)response.Usage.InputTokenCount.Value
                : null;
            audit.OutputTokens = response.Usage.OutputTokenCount.HasValue
                ? (int?)response.Usage.OutputTokenCount.Value
                : null;
            audit.TotalTokens = response.Usage.TotalTokenCount.HasValue
                ? (int?)response.Usage.TotalTokenCount.Value
                : null;
        }

        if (_options.CurrentValue.PersistResponses && response?.Data != null && !string.IsNullOrEmpty(response.Data.ToString()))
        {
            audit.ResponseSnapshot = ApplyRedaction(response.Data.ToString());
        }

        // Queue just the persistence operation
        var workItem = new BackgroundWorkItem(
            Name: "CompleteAuditLog",
            CorrelationId: audit.Id.ToString(),
            RunAsync: async (sp, token) =>
            {
                var repository = sp.GetRequiredService<IAiAuditLogRepository>();
                await repository.SaveAsync(audit, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug("Queued CompleteAuditLog for audit {AuditLogId} (Duration: {Duration}ms, Tokens: {TotalTokens})",
            audit.Id, audit.Duration?.TotalMilliseconds, audit.TotalTokens);
    }

    /// <inheritdoc />
    public async ValueTask QueueRecordAuditLogFailureAsync(
        AiAuditLog audit,
        Exception exception,
        CancellationToken ct = default)
    {
        // Do all the business logic NOW, before queuing
        audit.EndTime = DateTime.UtcNow;
        audit.Status = AiAuditLogStatus.Failed;
        audit.ErrorMessage = exception.Message;
        audit.ErrorCategory = CategorizeError(exception);

        // Log immediately based on options
        if (_options.CurrentValue.PersistFailureDetails)
        {
            _logger.LogError(exception,
                "AuditLog {AuditLogId} failed with error: {ErrorMessage} (Duration: {Duration}ms)",
                audit.Id, exception.Message, audit.Duration?.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "AuditLog {AuditLogId} failed with error: {ErrorMessage} (Duration: {Duration}ms)",
                audit.Id, exception.Message, audit.Duration?.TotalMilliseconds);
        }

        // Queue just the persistence operation
        var workItem = new BackgroundWorkItem(
            Name: "RecordAuditLogFailure",
            CorrelationId: audit.Id.ToString(),
            RunAsync: async (sp, token) =>
            {
                var repository = sp.GetRequiredService<IAiAuditLogRepository>();
                await repository.SaveAsync(audit, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug("Queued RecordAuditLogFailure for audit {AuditLogId}", audit.Id);
    }

    /// <inheritdoc />
    public async Task<AiAuditLog?> GetAuditLogAsync(Guid id, CancellationToken ct = default)
        => await _auditLogRepository.GetByIdAsync(id, ct);

    /// <inheritdoc />
    public async Task<(IEnumerable<AiAuditLog>, int Total)> GetAuditLogsPagedAsync(
        AiAuditLogFilter filter,
        int skip,
        int take,
        CancellationToken ct = default)
        => await _auditLogRepository.GetPagedAsync(filter, skip, take, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<AiAuditLog>> GetEntityHistoryAsync(
        string entityId,
        string entityType,
        int limit,
        CancellationToken ct = default)
        => await _auditLogRepository.GetByEntityIdAsync(entityId, entityType, limit, ct);

    /// <inheritdoc />
    public async Task<bool> DeleteAuditLogAsync(Guid id, CancellationToken ct = default)
        => await _auditLogRepository.DeleteAsync(id, ct);

    /// <inheritdoc />
    public async Task<int> CleanupOldAuditLogsAsync(CancellationToken ct = default)
    {
        var retentionDays = _options.CurrentValue.RetentionDays;
        var threshold = DateTime.UtcNow.AddDays(-retentionDays);

        int deleted = await _auditLogRepository.DeleteOlderThanAsync(threshold, ct);

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} AI traces older than {Days} days (threshold: {Threshold})",
                deleted, retentionDays, threshold);
        }

        return deleted;
    }

    private static AiAuditLogErrorCategory CategorizeError(Exception exception)
    {
        // Basic error categorization - can be enhanced based on exception types
        var exceptionType = exception.GetType().Name;
        var message = exception.Message.ToLower();

        if (message.Contains("unauthorized") || message.Contains("authentication") || message.Contains("api key"))
        {
            return AiAuditLogErrorCategory.Authentication;
        }

        if (message.Contains("rate limit") || message.Contains("quota"))
        {
            return AiAuditLogErrorCategory.RateLimiting;
        }

        if (message.Contains("model not found") || message.Contains("model") && message.Contains("not available"))
        {
            return AiAuditLogErrorCategory.ModelNotFound;
        }

        if (message.Contains("invalid") || message.Contains("bad request"))
        {
            return AiAuditLogErrorCategory.InvalidRequest;
        }

        if (message.Contains("server error") || message.Contains("500"))
        {
            return AiAuditLogErrorCategory.ServerError;
        }

        if (message.Contains("network") || message.Contains("timeout") || message.Contains("connection"))
        {
            return AiAuditLogErrorCategory.NetworkError;
        }

        if (message.Contains("context") && message.Contains("resolution"))
        {
            return AiAuditLogErrorCategory.ContextResolution;
        }

        if (message.Contains("tool"))
        {
            return AiAuditLogErrorCategory.ToolExecution;
        }

        return AiAuditLogErrorCategory.Unknown;
    }
    
    private string? ApplyRedaction(string? input)
    {
        if (string.IsNullOrEmpty(input) || _options.CurrentValue.RedactionPatterns.Count == 0)
            return input;

        var result = input;
        foreach (var pattern in _options.CurrentValue.RedactionPatterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply redaction pattern: {Pattern}", pattern);
            }
        }
        return result;
    }
}
