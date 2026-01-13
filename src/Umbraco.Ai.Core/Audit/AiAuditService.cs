using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Service implementation for AI governance tracing operations.
/// </summary>
internal sealed class AiAuditService : IAiAuditService
{
    private readonly IAiAuditRepository _auditRepository;
    private readonly IAiAuditActivityRepository _activityRepository;
    private readonly IAiProfileService _profileService;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly IOptionsMonitor<AiAuditOptions> _options;
    private readonly ILogger<AiAuditService> _logger;

    public AiAuditService(
        IAiAuditRepository auditRepository,
        IAiAuditActivityRepository activityRepository,
        IAiProfileService profileService,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptionsMonitor<AiAuditOptions> options,
        ILogger<AiAuditService> logger)
    {
        _auditRepository = auditRepository;
        _activityRepository = activityRepository;
        _profileService = profileService;
        _securityAccessor = securityAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiAudit> StartAuditAsync(
        AiAuditContext context,
        CancellationToken ct)
    {
        // Get profile details for additional information (optional)
        AiProfile? profile = null;
        if (context.ProfileId != Guid.Empty)
        {
            profile = await _profileService.GetProfileAsync(context.ProfileId, ct);
            if (profile is null)
            {
                _logger.LogDebug("Profile {ProfileId} not found, using context values for audit", context.ProfileId);
            }
        }

        // Determine operation type
        string operationType = context.Capability switch
        {
            AiCapability.Chat => "chat",
            AiCapability.Embedding => "embedding",
            _ => "unknown"
        };

        // Get current user
        var backOfficeIdentity = _securityAccessor?.BackOfficeSecurity?.CurrentUser;

        // Create audit record
        var audit = new AiAudit
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Status = AiAuditStatus.Running,
            UserId = backOfficeIdentity?.Key.ToString() ?? "-1",
            UserName = backOfficeIdentity?.Name ?? "unknown",
            EntityId = context.EntityId,
            EntityType = context.EntityType,
            OperationType = operationType,
            ProfileId = profile?.Id ?? context.ProfileId,
            ProfileAlias = profile?.Alias ?? context.ProfileAlias ?? "unknown",
            ProviderId = profile?.Model.ProviderId ?? context.ProviderId ?? "unknown",
            ModelId = profile?.Model.ModelId ?? context.ModelId ?? "unknown",
            FeatureType = context.FeatureType,
            FeatureId = context.FeatureId,
            DetailLevel = _options.CurrentValue.DetailLevel
        };

        // Capture prompt snapshot if configured
        if (_options.CurrentValue.PersistPrompts && context.Prompt is not null)
        {
            audit.PromptSnapshot = FormatPromptSnapshot(context.Prompt, context.Capability);
            _logger.LogDebug("Captured prompt snapshot for audit {TraceId}: {Length} characters",
                audit.Id, audit.PromptSnapshot?.Length ?? 0);
        }

        await _auditRepository.SaveAsync(audit, ct);

        _logger.LogDebug(
            "Started audit {TraceId} for {OperationType} operation by user {UserId}",
            audit.Id, operationType, audit.UserId);

        return audit;
    }

    /// <inheritdoc />
    public async Task CompleteAuditAsync(
        Guid auditId,
        object? response,
        CancellationToken ct)
    {
        var audit = await _auditRepository.GetByIdAsync(auditId, ct);
        if (audit is null)
        {
            _logger.LogWarning("Audit {AuditId} not found for completion", auditId);
            return;
        }

        audit.EndTime = DateTime.UtcNow;
        audit.Status = AiAuditStatus.Succeeded;

        // Extract token counts from response
        if (response is ChatResponse chatResponse && chatResponse.Usage is not null)
        {
            audit.InputTokens = chatResponse.Usage.InputTokenCount.HasValue
                ? (int?)chatResponse.Usage.InputTokenCount.Value
                : null;
            audit.OutputTokens = chatResponse.Usage.OutputTokenCount.HasValue
                ? (int?)chatResponse.Usage.OutputTokenCount.Value
                : null;
            audit.TotalTokens = chatResponse.Usage.TotalTokenCount.HasValue
                ? (int?)chatResponse.Usage.TotalTokenCount.Value
                : null;

            // Optionally persist response snapshot
            if (_options.CurrentValue.PersistResponses && !string.IsNullOrEmpty(chatResponse.Text))
            {
                audit.ResponseSnapshot = chatResponse.Text;
            }
        }
        else if (response is GeneratedEmbeddings<Embedding<float>> embeddingResponse && embeddingResponse.Usage is not null)
        {
            audit.InputTokens = embeddingResponse.Usage.InputTokenCount.HasValue
                ? (int?)embeddingResponse.Usage.InputTokenCount.Value
                : null;
            audit.OutputTokens = embeddingResponse.Usage.OutputTokenCount.HasValue
                ? (int?)embeddingResponse.Usage.OutputTokenCount.Value
                : null;
            audit.TotalTokens = embeddingResponse.Usage.TotalTokenCount.HasValue
                ? (int?)embeddingResponse.Usage.TotalTokenCount.Value
                : null;
        }

        await _auditRepository.SaveAsync(audit, ct);

        _logger.LogDebug(
            "Completed audit {TraceId} with status {Status} (Duration: {Duration}ms, Tokens: {TotalTokens})",
            audit.Id, AiAuditStatus.Succeeded, audit.Duration?.TotalMilliseconds, audit.TotalTokens);
    }

    /// <inheritdoc />
    public async Task RecordAuditFailureAsync(
        Guid auditId,
        Exception exception,
        CancellationToken ct)
    {
        var audit = await _auditRepository.GetByIdAsync(auditId, ct);
        if (audit is null)
        {
            _logger.LogWarning("Audit {AuditId} not found for failure recording", auditId);
            return;
        }

        audit.EndTime = DateTime.UtcNow;
        audit.Status = AiAuditStatus.Failed;
        audit.ErrorMessage = exception.Message;
        audit.ErrorCategory = CategorizeError(exception);

        await _auditRepository.SaveAsync(audit, ct);

        if (_options.CurrentValue.PersistFailureDetails)
        {
            _logger.LogError(exception,
                "Audit {TraceId} failed with error: {ErrorMessage} (Duration: {Duration}ms)",
                audit.Id, exception.Message, audit.Duration?.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "Audit {TraceId} failed with error: {ErrorMessage} (Duration: {Duration}ms)",
                audit.Id, exception.Message, audit.Duration?.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task RecordActivityAsync(
        Guid auditId,
        AiAuditActivityType activityType,
        string activityName,
        Func<Task<object?>> operation,
        CancellationToken ct)
    {
        var options = _options.CurrentValue;

        // Check if we should record this span based on detail level
        bool shouldRecord = options.DetailLevel switch
        {
            AiAuditDetailLevel.Audit => false,
            AiAuditDetailLevel.FailuresOnly => false, // Will record if operation fails
            AiAuditDetailLevel.Sampled => Random.Shared.NextDouble() < options.SamplingRate,
            AiAuditDetailLevel.Full => true,
            _ => false
        };

        if (!shouldRecord)
        {
            // Just execute without recording
            await operation();
            return;
        }

        // Get existing activities to determine sequence number
        var existingActivities = await _activityRepository.GetByAuditIdAsync(auditId, ct);
        int sequenceNumber = existingActivities.Count();

        var activity = new AiAuditActivity
        {
            Id = Guid.NewGuid(),
            AuditId = auditId,
            ActivityName = activityName,
            ActivityType = activityType,
            SequenceNumber = sequenceNumber,
            StartTime = DateTime.UtcNow,
            Status = AiAuditActivityStatus.Running
        };

        try
        {
            object? result = await operation();
            activity.EndTime = DateTime.UtcNow;
            activity.Status = AiAuditActivityStatus.Succeeded;
            // Could serialize result if needed
        }
        catch (Exception ex)
        {
            activity.EndTime = DateTime.UtcNow;
            activity.Status = AiAuditActivityStatus.Failed;
            activity.ErrorData = ex.Message;

            // Always record failures
            await _activityRepository.SaveAsync(activity, ct);
            throw;
        }

        await _activityRepository.SaveAsync(activity, ct);
    }

    /// <inheritdoc />
    public async Task<AiAudit?> GetAuditAsync(Guid id, CancellationToken ct, bool includeActivities = false)
    {
        var audit = await _auditRepository.GetByIdAsync(id, ct);

        if (audit is not null && includeActivities)
        {
            var activities = await _activityRepository.GetByAuditIdAsync(id, ct);
            audit.Activities = activities.ToList();
        }

        return audit;
    }

    /// <inheritdoc />
    public async Task<AiAudit?> GetAuditByTraceIdAsync(string auditId, CancellationToken ct)
        => await _auditRepository.GetByTraceIdAsync(auditId, ct);

    /// <inheritdoc />
    public async Task<(IEnumerable<AiAudit>, int Total)> GetAuditsPagedAsync(
        AiAuditFilter filter,
        int skip,
        int take,
        CancellationToken ct)
        => await _auditRepository.GetPagedAsync(filter, skip, take, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<AiAudit>> GetEntityHistoryAsync(
        string entityId,
        string entityType,
        int limit,
        CancellationToken ct)
        => await _auditRepository.GetByEntityIdAsync(entityId, entityType, limit, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<AiAuditActivity>> GetAuditActivitiesAsync(Guid auditId, CancellationToken ct)
        => await _activityRepository.GetByAuditIdAsync(auditId, ct);

    /// <inheritdoc />
    public async Task<bool> DeleteAuditAsync(Guid id, CancellationToken ct)
        => await _auditRepository.DeleteAsync(id, ct);

    /// <inheritdoc />
    public async Task<int> CleanupOldAuditsAsync(CancellationToken ct)
    {
        var retentionDays = _options.CurrentValue.RetentionDays;
        var threshold = DateTime.UtcNow.AddDays(-retentionDays);

        int deleted = await _auditRepository.DeleteOlderThanAsync(threshold, ct);

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

    private static AiAuditErrorCategory CategorizeError(Exception exception)
    {
        // Basic error categorization - can be enhanced based on exception types
        var exceptionType = exception.GetType().Name;
        var message = exception.Message.ToLower();

        if (message.Contains("unauthorized") || message.Contains("authentication") || message.Contains("api key"))
        {
            return AiAuditErrorCategory.Authentication;
        }

        if (message.Contains("rate limit") || message.Contains("quota"))
        {
            return AiAuditErrorCategory.RateLimiting;
        }

        if (message.Contains("model not found") || message.Contains("model") && message.Contains("not available"))
        {
            return AiAuditErrorCategory.ModelNotFound;
        }

        if (message.Contains("invalid") || message.Contains("bad request"))
        {
            return AiAuditErrorCategory.InvalidRequest;
        }

        if (message.Contains("server error") || message.Contains("500"))
        {
            return AiAuditErrorCategory.ServerError;
        }

        if (message.Contains("network") || message.Contains("timeout") || message.Contains("connection"))
        {
            return AiAuditErrorCategory.NetworkError;
        }

        if (message.Contains("context") && message.Contains("resolution"))
        {
            return AiAuditErrorCategory.ContextResolution;
        }

        if (message.Contains("tool"))
        {
            return AiAuditErrorCategory.ToolExecution;
        }

        return AiAuditErrorCategory.Unknown;
    }
}
