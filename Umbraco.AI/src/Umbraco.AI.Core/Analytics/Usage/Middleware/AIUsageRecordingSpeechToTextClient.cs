using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.Analytics.Usage.Middleware;

/// <summary>
/// Speech-to-text client that records usage data to the analytics system.
/// Reads tracking data from the inner <see cref="AITrackingSpeechToTextClient"/> if available.
/// </summary>
internal sealed class AIUsageRecordingSpeechToTextClient : AIBoundSpeechToTextClientBase
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIUsageRecordingService _usageRecordingService;
    private readonly IAIUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageRecordingSpeechToTextClient> _logger;

    public AIUsageRecordingSpeechToTextClient(
        ISpeechToTextClient innerClient,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIUsageRecordingService usageRecordingService,
        IAIUsageRecordFactory factory,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageRecordingSpeechToTextClient> logger)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Skip if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            return await base.GetTextAsync(audioSpeechStream, options, cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var succeeded = false;
        string? errorMessage = null;

        try
        {
            var response = await base.GetTextAsync(audioSpeechStream, options, cancellationToken);
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

            // Extract context from runtime context
            var usageContext = AIUsageContext.ExtractFromRuntimeContext(AICapability.SpeechToText, _runtimeContextAccessor.Context);

            // Convert to factory context
            var context = AIUsageRecordContext.FromUsageContext(usageContext);

            // Create result object - STT doesn't have token-based usage, record duration
            var result = new AIUsageRecordResult
            {
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
            _logger.LogError(ex, "Failed to record AI usage for speech-to-text transcription");
        }
    }
}
