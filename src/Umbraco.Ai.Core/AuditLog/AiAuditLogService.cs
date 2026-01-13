using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Service implementation for AI governance tracing operations.
/// </summary>
internal sealed class AiAuditLogService : IAiAuditLogService
{
    private readonly IAiAuditLogRepository _auditLogRepository;
    private readonly IAiProfileService _profileService;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly IOptionsMonitor<AiAuditLogOptions> _options;
    private readonly ILogger<AiAuditLogService> _logger;

    public AiAuditLogService(
        IAiAuditLogRepository auditLogRepository,
        IAiProfileService profileService,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptionsMonitor<AiAuditLogOptions> options,
        ILogger<AiAuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _profileService = profileService;
        _securityAccessor = securityAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiAuditLog> StartAuditLogAsync(
        AiAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        // Automatically detect parent from ambient scope
        var parentAuditLogId = AiAuditScope.Current?.AuditLogId;

        // Get profile details for additional information (optional)
        AiProfile? profile = null;
        
        if (context.ProfileId.HasValue)
        {
            profile = await _profileService.GetProfileAsync(context.ProfileId.Value, ct);
            if (profile is null)
            {
                _logger.LogDebug("Profile {ProfileId} not found, using context values for audit-log", context.ProfileId);
            }
        }

        // Get current user
        var backOfficeIdentity = _securityAccessor.BackOfficeSecurity?.CurrentUser;

        // Create audit-log record
        var audit = new AiAuditLog
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Status = AiAuditLogStatus.Running,
            UserId = backOfficeIdentity?.Key.ToString(),
            UserName = backOfficeIdentity?.Name ?? "anonymous",
            EntityId = context.EntityId,
            EntityType = context.EntityType,
            Capability = context.Capability, // Use capability directly instead of operation type string
            ProfileId = (Guid)(profile?.Id ?? context.ProfileId)!,
            ProfileAlias = profile?.Alias ?? context.ProfileAlias ?? "unknown",
            ProviderId = profile?.Model.ProviderId ?? context.ProviderId ?? "unknown",
            ModelId = profile?.Model.ModelId ?? context.ModelId ?? "unknown",
            FeatureType = context.FeatureType,
            FeatureId = context.FeatureId,
            DetailLevel = _options.CurrentValue.DetailLevel,
            ParentAuditLogId = parentAuditLogId, // Automatic parent detection
            Metadata = metadata ?? context.Metadata // Use provided metadata or context metadata
        };

        // Capture prompt snapshot if configured
        if (_options.CurrentValue.PersistPrompts && context.Prompt is not null)
        {
            var prompt = FormatPromptSnapshot(context.Prompt, context.Capability);
            prompt = ApplyRedaction(prompt);
            audit.PromptSnapshot = prompt;
            _logger.LogDebug("Captured prompt snapshot for audit-log {AuditLogId}: {Length} characters",
                audit.Id, prompt?.Length ?? 0);
        }

        await _auditLogRepository.SaveAsync(audit, ct);

        _logger.LogDebug(
            "Started audit-log {AuditLogId} for {Capability} operation by user {UserId} (Parent: {ParentId})",
            audit.Id, audit.Capability, audit.UserId, audit.ParentAuditLogId);

        return audit;
    }

    /// <inheritdoc />
    public async Task<AiAuditScopeHandle> StartAuditLogScopeAsync(
        AiAuditContext context,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var audit = await StartAuditLogAsync(context, metadata, ct);
        var scope = AiAuditScope.Begin(audit.Id);
        return new AiAuditScopeHandle(scope, audit);
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
        
        if (_options.CurrentValue.PersistResponses && !string.IsNullOrEmpty(response?.Text))
        {
            audit.ResponseSnapshot = ApplyRedaction(response.Text);
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

    private static string? FormatPromptSnapshot(object? promptObj, AiCapability capability)
    {
        if (promptObj is null)
        {
            return null;
        }

        try
        {
            return capability switch
            {
                AiCapability.Chat when promptObj is IEnumerable<ChatMessage> messages =>
                    string.Join("\n", messages.Select(m => $"[{m.Role}] {m.Text}")),

                AiCapability.Embedding when promptObj is IEnumerable<string> values =>
                    string.Join("\n", values.Select((v, i) => $"[{i}] {v}")),

                _ => promptObj.ToString()
            };
        }
        catch
        {
            // If formatting fails, return a fallback representation
            return $"[Unable to format {capability} prompt]";
        }
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
