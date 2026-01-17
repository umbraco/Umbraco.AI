using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Core.Analytics.Middleware;

/// <summary>
/// Chat client that records usage data to the analytics system.
/// Reads tracking data from the inner <see cref="AiTrackingChatClient"/> if available.
/// </summary>
internal sealed class AiUsageRecordingChatClient : AiBoundChatClientBase
{
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IBackOfficeSecurityAccessor _securityAccessor;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageRecordingChatClient> _logger;

    public AiUsageRecordingChatClient(
        IChatClient innerClient,
        IAiUsageRecordingService usageRecordingService,
        IBackOfficeSecurityAccessor securityAccessor,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageRecordingChatClient> logger)
        : base(innerClient)
    {
        _usageRecordingService = usageRecordingService;
        _securityAccessor = securityAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Skip if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            return await base.GetResponseAsync(chatMessages, options, cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var succeeded = false;
        string? errorMessage = null;
        ChatResponse? response = null;

        try
        {
            response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            succeeded = true;
            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Record usage asynchronously (fire and forget - don't block the response)
            _ = RecordUsageAsync(
                options,
                stopwatch.ElapsedMilliseconds,
                succeeded,
                errorMessage,
                cancellationToken);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Skip if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        var stopwatch = Stopwatch.StartNew();
        var succeeded = false;
        string? errorMessage = null;

        // Collect updates in a list (can't yield inside try-catch)
        var updates = new List<ChatResponseUpdate>();

        await using var enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                updates.Add(enumerator.Current);
            }

            succeeded = true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            errorMessage = ex.Message;

            // Record usage even on error (fire and forget)
            _ = RecordUsageAsync(
                options,
                stopwatch.ElapsedMilliseconds,
                succeeded,
                errorMessage,
                cancellationToken);

            throw;
        }

        stopwatch.Stop();

        // Record usage asynchronously (fire and forget)
        _ = RecordUsageAsync(
            options,
            stopwatch.ElapsedMilliseconds,
            succeeded,
            errorMessage,
            cancellationToken);

        // Yield collected updates
        foreach (var update in updates)
        {
            yield return update;
        }
    }

    private async Task RecordUsageAsync(
        ChatOptions? options,
        long durationMs,
        bool succeeded,
        string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            // Extract context from options
            var context = AiUsageContext.ExtractFromOptions(AiCapability.Chat, options);

            // Try to get tracking data from inner client
            var trackingClient = InnerClient.GetService<AiTrackingChatClient>();
            var usageDetails = trackingClient?.LastUsageDetails;

            // If we don't have usage details, we can't record (no token counts available)
            if (usageDetails == null)
            {
                _logger.LogDebug("No usage details available from tracking client, skipping usage recording");
                return;
            }

            // Get current user
            var backOfficeIdentity = _securityAccessor.BackOfficeSecurity?.CurrentUser;

            // Create usage record
            var record = new AiUsageRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Capability = AiCapability.Chat,
                UserId = _options.CurrentValue.IncludeUsageUserDimension ? backOfficeIdentity?.Key.ToString() : null,
                UserName = _options.CurrentValue.IncludeUsageUserDimension ? backOfficeIdentity?.Name : null,
                ProfileId = context.ProfileId ?? Guid.Empty,
                ProfileAlias = context.ProfileAlias ?? "unknown",
                ProviderId = context.ProviderId ?? "unknown",
                ModelId = context.ModelId ?? "unknown",
                FeatureType = _options.CurrentValue.IncludeUsageFeatureTypeDimension ? context.FeatureType : null,
                FeatureId = _options.CurrentValue.IncludeUsageFeatureTypeDimension ? context.FeatureId : null,
                EntityId = context.EntityId,
                EntityType = _options.CurrentValue.IncludeUsageEntityTypeDimension ? context.EntityType : null,
                InputTokens = (int)(usageDetails.InputTokenCount ?? 0),
                OutputTokens = (int)(usageDetails.OutputTokenCount ?? 0),
                TotalTokens = (int)(usageDetails.TotalTokenCount ?? 0),
                DurationMs = durationMs,
                Status = succeeded ? "Succeeded" : "Failed",
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _usageRecordingService.RecordUsageAsync(record, ct);
        }
        catch (Exception ex)
        {
            // Log but don't throw - recording failures shouldn't break the main operation
            _logger.LogError(ex, "Failed to record AI usage");
        }
    }
}
