using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.Analytics.Usage.Middleware;

/// <summary>
/// Chat client that records usage data to the analytics system.
/// Reads tracking data from the inner <see cref="AiTrackingChatClient"/> if available.
/// </summary>
internal sealed class AiUsageRecordingChatClient : AiBoundChatClientBase
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IAiUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageRecordingChatClient> _logger;

    public AiUsageRecordingChatClient(
        IChatClient innerClient,
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiUsageRecordingService usageRecordingService,
        IAiUsageRecordFactory factory,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageRecordingChatClient> logger)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _usageRecordingService = usageRecordingService;
        _factory = factory;
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
        Exception? capturedException = null;

        // We still collect updates for metrics, but yield immediately for true streaming.
        // The try-catch surrounds only MoveNextAsync() since yield is not allowed inside try-catch.
        var updates = new List<ChatResponseUpdate>();

        await using var enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatResponseUpdate? current;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    succeeded = true;
                    break;
                }
                current = enumerator.Current;
            }
            catch (Exception ex)
            {
                capturedException = ex;
                errorMessage = ex.Message;
                break;
            }

            updates.Add(current);
            yield return current;  // Yield immediately for true streaming!
        }

        stopwatch.Stop();

        // Record usage asynchronously (fire and forget)
        _ = RecordUsageAsync(
            stopwatch.ElapsedMilliseconds,
            succeeded,
            errorMessage,
            cancellationToken);

        // Re-throw any captured exception after recording metrics
        if (capturedException is not null)
        {
            throw capturedException;
        }
    }

    private async Task RecordUsageAsync(
        long durationMs,
        bool succeeded,
        string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            
            if (_runtimeContextAccessor.Context == null)
            {
                _logger.LogDebug("No runtime context available, skipping usage recording");
                return;
            }
            
            // Extract context from options
            var usageContext = AiUsageContext.ExtractFromRuntimeContext(AiCapability.Chat, _runtimeContextAccessor.Context);

            // Try to get tracking data from inner client
            var trackingClient = InnerClient.GetService<AiTrackingChatClient>();
            var usageDetails = trackingClient?.LastUsageDetails;

            // If we don't have usage details, we can't record (no token counts available)
            if (usageDetails == null)
            {
                _logger.LogDebug("No usage details available from tracking client, skipping usage recording");
                return;
            }

            // Convert to factory context
            var context = AiUsageRecordContext.FromUsageContext(usageContext);

            // Create result object
            var result = new AiUsageRecordResult
            {
                Usage = usageDetails,
                DurationMs = durationMs,
                Succeeded = succeeded,
                ErrorMessage = errorMessage
            };

            // Create record via factory (validates and captures user)
            var record = _factory.Create(context, result);

            // Queue for background persistence
            await _usageRecordingService.QueueRecordUsageAsync(record, ct);
        }
        catch (Exception ex)
        {
            // Log but don't throw - recording failures shouldn't break the main operation
            _logger.LogError(ex, "Failed to record AI usage");
        }
    }
}
