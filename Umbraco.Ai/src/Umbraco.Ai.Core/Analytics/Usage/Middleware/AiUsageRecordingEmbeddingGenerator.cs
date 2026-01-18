using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Analytics.Middleware;

/// <summary>
/// Embedding generator that records usage data to the analytics system.
/// Reads tracking data from the inner <see cref="AiTrackingEmbeddingGenerator{TInput,TEmbedding}"/> if available.
/// </summary>
internal sealed class AiUsageRecordingEmbeddingGenerator<TInput, TEmbedding> : AiBoundEmbeddingGeneratorBase<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IAiUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AiAnalyticsOptions> _options;
    private readonly ILogger<AiUsageRecordingEmbeddingGenerator<TInput, TEmbedding>> _logger;

    public AiUsageRecordingEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        IAiUsageRecordingService usageRecordingService,
        IAiUsageRecordFactory factory,
        IOptionsMonitor<AiAnalyticsOptions> options,
        ILogger<AiUsageRecordingEmbeddingGenerator<TInput, TEmbedding>> logger)
        : base(innerGenerator)
    {
        _usageRecordingService = usageRecordingService;
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Skip if analytics is disabled
        if (!_options.CurrentValue.Enabled)
        {
            return await base.GenerateAsync(values, options, cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var succeeded = false;
        string? errorMessage = null;
        GeneratedEmbeddings<TEmbedding>? result = null;

        try
        {
            result = await base.GenerateAsync(values, options, cancellationToken);
            succeeded = true;
            return result;
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

    private async Task RecordUsageAsync(
        EmbeddingGenerationOptions? options,
        long durationMs,
        bool succeeded,
        string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            // Extract context from options
            var usageContext = AiUsageContext.ExtractFromOptions(AiCapability.Embedding, options);

            // Try to get tracking data from inner generator
            var trackingGenerator = InnerGenerator.GetService<AiTrackingEmbeddingGenerator<TInput, TEmbedding>>();
            var usageDetails = trackingGenerator?.LastUsageDetails;

            // If we don't have usage details, we can't record (no token counts available)
            if (usageDetails == null)
            {
                _logger.LogDebug("No usage details available from tracking generator, skipping usage recording");
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
            _logger.LogError(ex, "Failed to record AI usage for embedding generation");
        }
    }
}
