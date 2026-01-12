using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// ActivityListener that captures OpenTelemetry Activities from Umbraco.Ai operations
/// and persists them to the local database for governance and tracing purposes.
/// </summary>
internal sealed class AiGovernanceActivityListener : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiGovernanceActivityListener> _logger;
    private readonly AiGovernanceOptions _options;

    public AiGovernanceActivityListener(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AiGovernanceOptions> options,
        ILogger<AiGovernanceActivityListener> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.CurrentValue;
        _logger = logger;

        _listener = new ActivityListener
        {
            ShouldListenTo = (source) => source.Name == AiTelemetrySource.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };

        ActivitySource.AddActivityListener(_listener);
        _logger.LogDebug("AI Governance ActivityListener registered for source: {SourceName}", AiTelemetrySource.ActivitySourceName);
    }

    private void OnActivityStarted(Activity activity)
    {
        if (!_options.Enabled)
        {
            return;
        }

        // Only trace top-level AI operations (chat/embedding requests)
        if (activity.OperationName != AiTelemetrySource.ChatRequestActivity &&
            activity.OperationName != AiTelemetrySource.EmbeddingRequestActivity)
        {
            return;
        }

        _logger.LogTrace("Activity started: {OperationName} (TraceId: {TraceId})",
            activity.OperationName, activity.TraceId);

        // Start trace asynchronously (fire-and-forget with error handling)
        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();

            try
            {
                var traceService = scope.ServiceProvider.GetRequiredService<IAiTraceService>();

                // Extract additional properties from Activity tags
                var additionalProperties = ExtractAdditionalPropertiesFromTags(activity);

                // Determine capability from operation name
                var capability = activity.OperationName == AiTelemetrySource.ChatRequestActivity
                    ? AiCapability.Chat
                    : AiCapability.Embedding;

                var trace = await traceService.StartTraceAsync(capability, additionalProperties, CancellationToken.None);

                // Store trace ID in Activity custom property for completion
                activity.SetCustomProperty("TraceRecordId", trace.Id);

                _logger.LogDebug("Trace record {TraceRecordId} created for Activity {TraceId}",
                    trace.Id, activity.TraceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start trace for Activity {ActivityId}", activity.Id);
            }
        });
    }

    private void OnActivityStopped(Activity activity)
    {
        if (!_options.Enabled)
        {
            return;
        }

        // Check if we have a trace record ID
        if (activity.GetCustomProperty("TraceRecordId") is not Guid traceId)
        {
            return;
        }

        _logger.LogTrace("Activity stopped: {OperationName} (TraceId: {TraceId}, Status: {Status})",
            activity.OperationName, activity.TraceId, activity.Status);

        // Complete trace asynchronously
        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();

            try
            {
                var traceService = scope.ServiceProvider.GetRequiredService<IAiTraceService>();

                var status = activity.Status == ActivityStatusCode.Error
                    ? AiTraceStatus.Failed
                    : AiTraceStatus.Succeeded;

                // Extract response data from Activity tags
                var response = ExtractResponseFromActivity(activity);
                var exception = ExtractExceptionFromActivity(activity);

                await traceService.CompleteTraceAsync(traceId, status, CancellationToken.None, response, exception);

                _logger.LogDebug("Trace record {TraceRecordId} completed with status {Status}",
                    traceId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete trace {TraceId}", traceId);
            }
        });
    }

    private Dictionary<string, object?>? ExtractAdditionalPropertiesFromTags(Activity activity)
    {
        var props = new Dictionary<string, object?>();

        foreach (var tag in activity.Tags)
        {
            props[tag.Key] = tag.Value;
        }

        return props.Count > 0 ? props : null;
    }

    private object? ExtractResponseFromActivity(Activity activity)
    {
        // Response object is typically not stored in Activity tags due to size
        // This would be populated by the middleware if needed
        // For now, we rely on the middleware to pass the actual response object
        return null;
    }

    private Exception? ExtractExceptionFromActivity(Activity activity)
    {
        // Check if activity has exception events
        foreach (var activityEvent in activity.Events)
        {
            if (activityEvent.Name == "exception")
            {
                // Extract exception details from tags
                string? exceptionMessage = null;
                string? exceptionType = null;

                foreach (var tag in activityEvent.Tags)
                {
                    if (tag.Key == "exception.message")
                    {
                        exceptionMessage = tag.Value?.ToString();
                    }
                    else if (tag.Key == "exception.type")
                    {
                        exceptionType = tag.Value?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(exceptionMessage))
                {
                    // Create a simple exception wrapper with the message
                    return new Exception($"{exceptionType}: {exceptionMessage}");
                }
            }
        }

        return null;
    }

    public void Dispose()
    {
        _listener?.Dispose();
        _logger.LogDebug("AI Governance ActivityListener disposed");
    }
}
