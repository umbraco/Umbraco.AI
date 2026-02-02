using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Analytics.Usage.Middleware;

/// <summary>
/// Embedding generator that records usage data to the analytics system.
/// Reads tracking data from the inner <see cref="AITrackingEmbeddingGenerator{TInput,TEmbedding}"/> if available.
/// </summary>
internal sealed class AIUsageRecordingEmbeddingGenerator<TInput, TEmbedding> : AIBoundEmbeddingGeneratorBase<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiUsageRecordingService _usageRecordingService;
    private readonly IAiUsageRecordFactory _factory;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _options;
    private readonly ILogger<AIUsageRecordingEmbeddingGenerator<TInput, TEmbedding>> _logger;

    public AIUsageRecordingEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiUsageRecordingService usageRecordingService,
        IAiUsageRecordFactory factory,
        IOptionsMonitor<AIAnalyticsOptions> options,
        ILogger<AIUsageRecordingEmbeddingGenerator<TInput, TEmbedding>> logger)
        : base(innerGenerator)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
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
            if (_runtimeContextAccessor.Context == null)
            {
                _logger.LogDebug("No runtime context available, skipping usage recording");
                return;
            }
            
            // Extract context from options
            var usageContext = AIUsageContext.ExtractFromRuntimeContext(AICapability.Embedding, _runtimeContextAccessor.Context);

            // Try to get tracking data from inner generator
            var trackingGenerator = InnerGenerator.GetService<AITrackingEmbeddingGenerator<TInput, TEmbedding>>();
            var usageDetails = trackingGenerator?.LastUsageDetails;

            // If we don't have usage details, we can't record (no token counts available)
            if (usageDetails == null)
            {
                _logger.LogDebug("No usage details available from tracking generator, skipping usage recording");
                return;
            }

            // Convert to factory context
            var context = AIUsageRecordContext.FromUsageContext(usageContext);

            // Create result object
            var result = new AIUsageRecordResult
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
