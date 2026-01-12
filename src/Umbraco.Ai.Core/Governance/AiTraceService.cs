using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Service implementation for AI governance tracing operations.
/// </summary>
internal sealed class AiTraceService : IAiTraceService
{
    private readonly IAiTraceRepository _traceRepository;
    private readonly IAiExecutionSpanRepository _spanRepository;
    private readonly IAiProfileService _profileService;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly IOptionsMonitor<AiGovernanceOptions> _options;
    private readonly ILogger<AiTraceService> _logger;

    public AiTraceService(
        IAiTraceRepository traceRepository,
        IAiExecutionSpanRepository spanRepository,
        IAiProfileService profileService,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptionsMonitor<AiGovernanceOptions> options,
        ILogger<AiTraceService> logger)
    {
        _traceRepository = traceRepository;
        _spanRepository = spanRepository;
        _profileService = profileService;
        _securityAccessor = securityAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiTrace> StartTraceAsync(
        AiCapability capability,
        IDictionary<string, object?>? additionalProperties,
        CancellationToken ct)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            throw new InvalidOperationException("No active Activity found. Ensure telemetry middleware is configured.");
        }

        // Extract profile ID from additional properties (using telemetry tag names)
        Guid profileId = Guid.Empty;
        if (additionalProperties?.TryGetValue(AiTelemetrySource.ProfileIdTag, out object? profileIdObj) == true)
        {
            if (profileIdObj is Guid guid)
            {
                profileId = guid;
            }
            else if (profileIdObj is string profileIdStr && Guid.TryParse(profileIdStr, out Guid parsed))
            {
                profileId = parsed;
            }
        }

        // Extract profile alias and provider ID directly from tags (in case profile lookup fails)
        string? profileAlias = additionalProperties?.TryGetValue(AiTelemetrySource.ProfileAliasTag, out object? aliasObj) == true
            ? aliasObj?.ToString()
            : null;

        string? providerId = additionalProperties?.TryGetValue(AiTelemetrySource.ProviderIdTag, out object? providerObj) == true
            ? providerObj?.ToString()
            : null;

        // Get model ID from tags
        string? modelId = additionalProperties?.TryGetValue(AiTelemetrySource.ModelIdTag, out object? modelObj) == true
            ? modelObj?.ToString()
            : null;

        // Get profile details for additional information (optional)
        AiProfile? profile = null;
        if (profileId != Guid.Empty)
        {
            profile = await _profileService.GetProfileAsync(profileId, ct);
        }

        // Use profile if found, otherwise use tag values directly
        if (profile is null && profileId != Guid.Empty)
        {
            _logger.LogDebug("Profile {ProfileId} not found, using tag values for trace", profileId);
        }

        // Get current user
        var backOfficeIdentity = _securityAccessor.BackOfficeSecurity?.CurrentUser;
        string userId = backOfficeIdentity?.Id.ToString() ?? "anonymous";
        string? userName = backOfficeIdentity?.Name;

        // Extract entity context from additional properties
        string? entityId = additionalProperties?.TryGetValue(AiTelemetrySource.EntityIdTag, out object? entityIdObj) == true
            ? entityIdObj?.ToString()
            : null;

        string? entityType = additionalProperties?.TryGetValue(AiTelemetrySource.EntityTypeTag, out object? entityTypeObj) == true
            ? entityTypeObj?.ToString()
            : null;

        // Determine operation type
        string operationType = capability switch
        {
            AiCapability.Chat => "chat",
            AiCapability.Embedding => "embedding",
            _ => "unknown"
        };

        if (additionalProperties?.TryGetValue(AiTelemetrySource.OperationTypeTag, out object? opTypeObj) == true)
        {
            operationType = opTypeObj?.ToString() ?? operationType;
        }

        // Create trace record
        var trace = new AiTrace
        {
            Id = Guid.NewGuid(),
            TraceId = activity.TraceId.ToHexString(),
            SpanId = activity.SpanId.ToHexString(),
            StartTime = DateTime.UtcNow,
            Status = AiTraceStatus.Running,
            UserId = userId,
            UserName = userName,
            EntityId = entityId,
            EntityType = entityType,
            OperationType = operationType,
            ProfileId = profile?.Id ?? profileId,
            ProfileAlias = profile?.Alias ?? profileAlias ?? "unknown",
            ProviderId = profile?.Model.ProviderId ?? providerId ?? "unknown",
            ModelId = profile?.Model.ModelId ?? modelId ?? "unknown",
            DetailLevel = _options.CurrentValue.DetailLevel
        };

        await _traceRepository.SaveAsync(trace, ct);

        _logger.LogDebug(
            "Started trace {TraceId} for {OperationType} operation by user {UserId}",
            trace.TraceId, operationType, userId);

        return trace;
    }

    /// <inheritdoc />
    public async Task CompleteTraceAsync(
        Guid traceId,
        AiTraceStatus status,
        CancellationToken ct,
        object? response = null,
        Exception? exception = null)
    {
        var trace = await _traceRepository.GetByIdAsync(traceId, ct);
        if (trace is null)
        {
            _logger.LogWarning("Trace {TraceId} not found for completion", traceId);
            return;
        }

        trace.EndTime = DateTime.UtcNow;
        trace.Status = status;

        // Extract token counts from response
        if (response is ChatResponse chatResponse && chatResponse.Usage is not null)
        {
            trace.InputTokens = chatResponse.Usage.InputTokenCount.HasValue
                ? (int?)chatResponse.Usage.InputTokenCount.Value
                : null;
            trace.OutputTokens = chatResponse.Usage.OutputTokenCount.HasValue
                ? (int?)chatResponse.Usage.OutputTokenCount.Value
                : null;
            trace.TotalTokens = chatResponse.Usage.TotalTokenCount.HasValue
                ? (int?)chatResponse.Usage.TotalTokenCount.Value
                : null;

            // Optionally persist response snapshot
            if (_options.CurrentValue.PersistResponses && !string.IsNullOrEmpty(chatResponse.Text))
            {
                trace.ResponseSnapshot = chatResponse.Text;
            }
        }

        // Handle exceptions and error categorization
        if (exception is not null)
        {
            trace.ErrorMessage = exception.Message;
            trace.ErrorCategory = CategorizeError(exception);

            if (_options.CurrentValue.PersistFailureDetails)
            {
                _logger.LogError(exception, "Trace {TraceId} failed with error: {ErrorMessage}",
                    trace.TraceId, exception.Message);
            }
        }

        await _traceRepository.SaveAsync(trace, ct);

        _logger.LogDebug(
            "Completed trace {TraceId} with status {Status} (Duration: {Duration}ms, Tokens: {TotalTokens})",
            trace.TraceId, status, trace.Duration?.TotalMilliseconds, trace.TotalTokens);
    }

    /// <inheritdoc />
    public async Task RecordSpanAsync(
        Guid traceId,
        AiExecutionSpanType spanType,
        string spanName,
        Func<Task<object?>> operation,
        CancellationToken ct)
    {
        var options = _options.CurrentValue;

        // Check if we should record this span based on detail level
        bool shouldRecord = options.DetailLevel switch
        {
            AiTraceDetailLevel.Audit => false,
            AiTraceDetailLevel.FailuresOnly => false, // Will record if operation fails
            AiTraceDetailLevel.Sampled => Random.Shared.NextDouble() < options.SamplingRate,
            AiTraceDetailLevel.Full => true,
            _ => false
        };

        if (!shouldRecord)
        {
            // Just execute without recording
            await operation();
            return;
        }

        // Get existing spans to determine sequence number
        var existingSpans = await _spanRepository.GetByTraceIdAsync(traceId, ct);
        int sequenceNumber = existingSpans.Count();

        var span = new AiExecutionSpan
        {
            Id = Guid.NewGuid(),
            TraceId = traceId,
            SpanId = Activity.Current?.SpanId.ToHexString() ?? string.Empty,
            ParentSpanId = Activity.Current?.ParentSpanId.ToHexString(),
            SpanName = spanName,
            SpanType = spanType,
            SequenceNumber = sequenceNumber,
            StartTime = DateTime.UtcNow,
            Status = AiExecutionSpanStatus.Running
        };

        try
        {
            object? result = await operation();
            span.EndTime = DateTime.UtcNow;
            span.Status = AiExecutionSpanStatus.Succeeded;
            // Could serialize result if needed
        }
        catch (Exception ex)
        {
            span.EndTime = DateTime.UtcNow;
            span.Status = AiExecutionSpanStatus.Failed;
            span.ErrorData = ex.Message;

            // Always record failures
            await _spanRepository.SaveAsync(span, ct);
            throw;
        }

        await _spanRepository.SaveAsync(span, ct);
    }

    /// <inheritdoc />
    public async Task<AiTrace?> GetTraceAsync(Guid id, CancellationToken ct, bool includeSpans = false)
    {
        var trace = await _traceRepository.GetByIdAsync(id, ct);

        if (trace is not null && includeSpans)
        {
            var spans = await _spanRepository.GetByTraceIdAsync(id, ct);
            trace.Spans = spans.ToList();
        }

        return trace;
    }

    /// <inheritdoc />
    public async Task<AiTrace?> GetTraceByTraceIdAsync(string traceId, CancellationToken ct)
        => await _traceRepository.GetByTraceIdAsync(traceId, ct);

    /// <inheritdoc />
    public async Task<(IEnumerable<AiTrace>, int Total)> GetTracesPagedAsync(
        AiTraceFilter filter,
        int skip,
        int take,
        CancellationToken ct)
        => await _traceRepository.GetPagedAsync(filter, skip, take, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<AiTrace>> GetEntityHistoryAsync(
        string entityId,
        string entityType,
        int limit,
        CancellationToken ct)
        => await _traceRepository.GetByEntityIdAsync(entityId, entityType, limit, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<AiExecutionSpan>> GetExecutionSpansAsync(Guid traceId, CancellationToken ct)
        => await _spanRepository.GetByTraceIdAsync(traceId, ct);

    /// <inheritdoc />
    public async Task<bool> DeleteTraceAsync(Guid id, CancellationToken ct)
        => await _traceRepository.DeleteAsync(id, ct);

    /// <inheritdoc />
    public async Task<int> CleanupOldTracesAsync(CancellationToken ct)
    {
        var retentionDays = _options.CurrentValue.RetentionDays;
        var threshold = DateTime.UtcNow.AddDays(-retentionDays);

        int deleted = await _traceRepository.DeleteOlderThanAsync(threshold, ct);

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} AI traces older than {Days} days (threshold: {Threshold})",
                deleted, retentionDays, threshold);
        }

        return deleted;
    }

    private static AiTraceErrorCategory CategorizeError(Exception exception)
    {
        // Basic error categorization - can be enhanced based on exception types
        var exceptionType = exception.GetType().Name;
        var message = exception.Message.ToLower();

        if (message.Contains("unauthorized") || message.Contains("authentication") || message.Contains("api key"))
        {
            return AiTraceErrorCategory.Authentication;
        }

        if (message.Contains("rate limit") || message.Contains("quota"))
        {
            return AiTraceErrorCategory.RateLimiting;
        }

        if (message.Contains("model not found") || message.Contains("model") && message.Contains("not available"))
        {
            return AiTraceErrorCategory.ModelNotFound;
        }

        if (message.Contains("invalid") || message.Contains("bad request"))
        {
            return AiTraceErrorCategory.InvalidRequest;
        }

        if (message.Contains("server error") || message.Contains("500"))
        {
            return AiTraceErrorCategory.ServerError;
        }

        if (message.Contains("network") || message.Contains("timeout") || message.Contains("connection"))
        {
            return AiTraceErrorCategory.NetworkError;
        }

        if (message.Contains("context") && message.Contains("resolution"))
        {
            return AiTraceErrorCategory.ContextResolution;
        }

        if (message.Contains("tool"))
        {
            return AiTraceErrorCategory.ToolExecution;
        }

        return AiTraceErrorCategory.Unknown;
    }
}
