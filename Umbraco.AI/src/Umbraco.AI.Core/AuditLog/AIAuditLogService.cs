using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.TaskQueue;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Service implementation for AI governance tracing operations.
/// </summary>
internal sealed class AIAuditLogService : IAIAuditLogService
{
    private readonly IAIAuditLogRepository _auditLogRepository;
    private readonly IOptionsMonitor<AIAuditLogOptions> _options;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<AIAuditLogService> _logger;

    public AIAuditLogService(
        IAIAuditLogRepository auditLogRepository,
        IOptionsMonitor<AIAuditLogOptions> options,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILoggerFactory loggerFactory)
    {
        _auditLogRepository = auditLogRepository;
        _options = options;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = loggerFactory.CreateLogger<AIAuditLogService>();
    }

    /// <inheritdoc />
    public async Task<AIAuditLog> StartAuditLogAsync(AIAuditLog auditLog,
        CancellationToken ct = default)
    {
        // The AIAuditLog.Create factory method should have already set most properties.
        // This method just handles parent ID resolution and persists to the database.

        // Set parent ID from explicit parameter or auto-detect from ambient scope
        if (!auditLog.ParentAuditLogId.HasValue)
        {
            var resolvedParentId = AIAuditScope.Current?.AuditLogId;
            if (resolvedParentId.HasValue)
            {
                auditLog.ParentAuditLogId = resolvedParentId;
            }
        }

        // Ensure status is set to Running
        if (auditLog.Status != AIAuditLogStatus.Running)
        {
            auditLog.Status = AIAuditLogStatus.Running;
        }

        await _auditLogRepository.SaveAsync(auditLog, ct);

        _logger.LogDebug(
            "Started audit-log {AuditLogId} for {Capability} operation by user {UserId} (Parent: {ParentId})",
            auditLog.Id, auditLog.Capability, auditLog.UserId, auditLog.ParentAuditLogId);

        return auditLog;
    }

    /// <inheritdoc />
    public async Task CompleteAuditLogAsync(
        AIAuditLog audit,
        AIAuditPrompt? prompt,
        AIAuditResponse? response,
        CancellationToken ct = default)
    {

        audit.EndTime = DateTime.UtcNow;
        audit.Status = AIAuditLogStatus.Succeeded;

        CapturePromptSnapshot(audit, prompt);

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

        if (_options.CurrentValue.PersistResponses && response?.Data != null)
        {
            var formattedResponse = FormatResponseSnapshot(response.Data);
            if (!string.IsNullOrEmpty(formattedResponse))
            {
                audit.ResponseSnapshot = ApplyRedaction(formattedResponse);
            }
        }

        await _auditLogRepository.SaveAsync(audit, ct);

        _logger.LogDebug(
            "Completed audit-log {AuditLogId} with status {Status} (Duration: {Duration}ms, Tokens: {TotalTokens})",
            audit.Id, AIAuditLogStatus.Succeeded, audit.Duration?.TotalMilliseconds, audit.TotalTokens);
    }

    /// <inheritdoc />
    public async Task RecordAuditLogFailureAsync(
        AIAuditLog audit,
        AIAuditPrompt? prompt,
        Exception exception,
        CancellationToken ct = default)
    {

        audit.EndTime = DateTime.UtcNow;

        CapturePromptSnapshot(audit, prompt);

        if (exception is AIGuardrailBlockedException)
        {
            audit.Status = AIAuditLogStatus.Blocked;
            audit.ErrorCategory = AIAuditLogErrorCategory.GuardrailBlocked;
        }
        else
        {
            audit.Status = AIAuditLogStatus.Failed;
            audit.ErrorCategory = CategorizeError(exception);
        }

        audit.ErrorMessage = exception.Message;

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
        AIAuditLog auditLog,
        CancellationToken ct = default)
    {
        // IMPORTANT: Resolve parent ID from ambient scope NOW, before queuing,
        // because AuditScope.Current won't be available in the background worker context
        if (!auditLog.ParentAuditLogId.HasValue)
        {
            var resolvedParentId = AIAuditScope.Current?.AuditLogId;
            if (resolvedParentId.HasValue)
            {
                auditLog.ParentAuditLogId = resolvedParentId.Value;
            }
        }

        // Ensure status is set to Running
        if (auditLog.Status != AIAuditLogStatus.Running)
        {
            auditLog.Status = AIAuditLogStatus.Running;
        }

        // Queue the persistence operation (all business logic already resolved above)
        var workItem = new BackgroundWorkItem(
            Name: "StartAuditLog",
            CorrelationId: auditLog.Id.ToString(),
            RunAsync: async (sp, token) =>
            {
                var repository = sp.GetRequiredService<IAIAuditLogRepository>();
                await repository.SaveAsync(auditLog, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug("Queued StartAuditLog for audit {AuditLogId} (Parent: {ParentId})",
            auditLog.Id, auditLog.ParentAuditLogId);
    }

    /// <inheritdoc />
    public async ValueTask QueueCompleteAuditLogAsync(
        AIAuditLog audit,
        AIAuditPrompt? prompt,
        AIAuditResponse? response,
        CancellationToken ct = default)
    {
        // Do all the business logic NOW, before queuing
        audit.EndTime = DateTime.UtcNow;
        audit.Status = AIAuditLogStatus.Succeeded;

        CapturePromptSnapshot(audit, prompt);

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

        if (_options.CurrentValue.PersistResponses && response?.Data != null)
        {
            var formattedResponse = FormatResponseSnapshot(response.Data);
            if (!string.IsNullOrEmpty(formattedResponse))
            {
                audit.ResponseSnapshot = ApplyRedaction(formattedResponse);
            }
        }

        // Queue just the persistence operation
        var workItem = new BackgroundWorkItem(
            Name: "CompleteAuditLog",
            CorrelationId: audit.Id.ToString(),
            RunAsync: async (sp, token) =>
            {
                var repository = sp.GetRequiredService<IAIAuditLogRepository>();
                await repository.SaveAsync(audit, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug("Queued CompleteAuditLog for audit {AuditLogId} (Duration: {Duration}ms, Tokens: {TotalTokens})",
            audit.Id, audit.Duration?.TotalMilliseconds, audit.TotalTokens);
    }

    /// <inheritdoc />
    public async ValueTask QueueRecordAuditLogFailureAsync(
        AIAuditLog audit,
        AIAuditPrompt? prompt,
        Exception exception,
        CancellationToken ct = default)
    {
        // Do all the business logic NOW, before queuing
        audit.EndTime = DateTime.UtcNow;

        CapturePromptSnapshot(audit, prompt);

        if (exception is AIGuardrailBlockedException)
        {
            audit.Status = AIAuditLogStatus.Blocked;
            audit.ErrorCategory = AIAuditLogErrorCategory.GuardrailBlocked;
        }
        else
        {
            audit.Status = AIAuditLogStatus.Failed;
            audit.ErrorCategory = CategorizeError(exception);
        }

        audit.ErrorMessage = exception.Message;

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
                var repository = sp.GetRequiredService<IAIAuditLogRepository>();
                await repository.SaveAsync(audit, token);
            });

        await _backgroundTaskQueue.QueueAsync(workItem, ct);

        _logger.LogDebug("Queued RecordAuditLogFailure for audit {AuditLogId}", audit.Id);
    }

    /// <inheritdoc />
    public async Task<AIAuditLog?> GetAuditLogAsync(Guid id, CancellationToken ct = default)
        => await _auditLogRepository.GetByIdAsync(id, ct);

    /// <inheritdoc />
    public async Task<(IEnumerable<AIAuditLog>, int Total)> GetAuditLogsPagedAsync(
        AIAuditLogFilter filter,
        int skip,
        int take,
        CancellationToken ct = default)
        => await _auditLogRepository.GetPagedAsync(filter, skip, take, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<AIAuditLog>> GetEntityHistoryAsync(
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

    private static AIAuditLogErrorCategory CategorizeError(Exception exception)
    {
        // Basic error categorization - can be enhanced based on exception types
        var exceptionType = exception.GetType().Name;
        var message = exception.Message.ToLower();

        if (message.Contains("unauthorized") || message.Contains("authentication") || message.Contains("api key"))
        {
            return AIAuditLogErrorCategory.Authentication;
        }

        if (message.Contains("rate limit") || message.Contains("quota"))
        {
            return AIAuditLogErrorCategory.RateLimiting;
        }

        if (message.Contains("model not found") || message.Contains("model") && message.Contains("not available"))
        {
            return AIAuditLogErrorCategory.ModelNotFound;
        }

        if (message.Contains("invalid") || message.Contains("bad request"))
        {
            return AIAuditLogErrorCategory.InvalidRequest;
        }

        if (message.Contains("server error") || message.Contains("500"))
        {
            return AIAuditLogErrorCategory.ServerError;
        }

        if (message.Contains("network") || message.Contains("timeout") || message.Contains("connection"))
        {
            return AIAuditLogErrorCategory.NetworkError;
        }

        if (message.Contains("context") && message.Contains("resolution"))
        {
            return AIAuditLogErrorCategory.ContextResolution;
        }

        if (message.Contains("tool"))
        {
            return AIAuditLogErrorCategory.ToolExecution;
        }

        return AIAuditLogErrorCategory.Unknown;
    }

    /// <summary>
    /// Formats response data for storage in the audit log.
    /// Handles ChatMessage types to include tool calls and other content types.
    /// </summary>
    private static string? FormatResponseSnapshot(object? data)
    {
        if (data is null)
        {
            return null;
        }

        return data switch
        {
            IEnumerable<ChatMessage> messages => AIChatMessageFormatter.FormatChatMessages(messages),
            ChatMessage message => AIChatMessageFormatter.FormatChatMessage(message),
            string text => text,
            _ => data.ToString()
        };
    }

    private void CapturePromptSnapshot(AIAuditLog audit, AIAuditPrompt? prompt)
    {
        if (!_options.CurrentValue.PersistPrompts || prompt?.Data is null)
            return;

        var formatted = AIAuditLogFactory.FormatPromptSnapshot(prompt.Data, prompt.Capability);
        if (!string.IsNullOrEmpty(formatted))
        {
            audit.PromptSnapshot = ApplyRedaction(formatted);

            _logger.LogDebug("Captured prompt snapshot for audit-log {AuditLogId}: {Length} characters",
                audit.Id, audit.PromptSnapshot?.Length ?? 0);
        }
    }

    private string? ApplyRedaction(string? input)
        => AIAuditLogRedactor.ApplyRedaction(input, _options.CurrentValue.RedactionPatterns, _logger);
}
