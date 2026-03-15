using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Core.AuditLog.Middleware;

internal sealed class AIAuditingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    public AIAuditingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
        : base(innerGenerator)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Start audit-log recording if enabled
        AIAuditScope? auditScope = null;
        AIAuditLog? auditLog = null;

        if (_auditLogOptions.CurrentValue.Enabled && _runtimeContextAccessor.Context is not null)
        {
            // Extract audit context from options and values
            var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
                AICapability.Embedding,
                _runtimeContextAccessor.Context,
                values.ToList());

            // Extract metadata from options if present
            Dictionary<string, string>? metadata = null;
            if (options?.AdditionalProperties?.TryGetValue(Constants.ContextKeys.LogKeys, out var logKeys) == true
                && logKeys is IEnumerable<string> keys)
            {
                metadata = keys.ToDictionary(
                    key => key,
                    key => options?.AdditionalProperties?[key]?.ToString() ?? string.Empty);
            }

            // Create audit-log entry using factory
            auditLog = _auditLogFactory.Create(
                auditLogContext,
                metadata,
                parentId: AIAuditScope.Current?.AuditLogId); // Capture parent from ambient scope

            // Create scope synchronously (for nested operation tracking)
            auditScope = AIAuditScope.Begin(auditLog.Id);

            // Capture TraceId from ambient Activity (created by OpenTelemetry middleware)
            auditLog.TraceId = Activity.Current?.TraceId.ToString();

            // Queue persistence in background (fire-and-forget)
            await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);
        }

        // Enrich the ambient Activity with Umbraco-specific tags (independent of audit logging)
        EnrichActivity(auditLog);

        try
        {
            var result = await base.GenerateAsync(values, options, cancellationToken);

            // Complete audit-log (if exists)
            if (auditLog is not null)
            {
                var trackingGenerator = InnerGenerator as AITrackingEmbeddingGenerator<string, Embedding<float>>;

                // Queue completion in background (fire-and-forget)
                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    new AIAuditResponse
                    {
                        Usage = trackingGenerator?.LastUsageDetails,
                        Data = trackingGenerator?.LastEmbeddings
                    },
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            // Record audit-log failure (if exists)
            if (auditLog is not null)
            {
                // Queue failure in background (fire-and-forget)
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog,
                    ex,
                    cancellationToken);
            }

            throw;
        }
        finally
        {
            // Dispose scope to restore previous audit context
            auditScope?.Dispose();
        }
    }

    /// <summary>
    /// Enriches the ambient <see cref="Activity"/> with Umbraco-specific tags.
    /// Tags are added to the span created by M.E.AI's OpenTelemetry middleware.
    /// This is a no-op when no Activity is active (i.e., OpenTelemetry is not configured).
    /// </summary>
    private void EnrichActivity(AIAuditLog? auditLog)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        // If audit log exists, use its already-extracted context
        if (auditLog is not null)
        {
            activity.SetTag(AITelemetry.Tags.ProfileId, auditLog.ProfileId.ToString());
            activity.SetTag(AITelemetry.Tags.ProfileAlias, auditLog.ProfileAlias);
            activity.SetTag(AITelemetry.Tags.AuditId, auditLog.Id.ToString());

            if (auditLog.UserId is not null)
            {
                activity.SetTag(AITelemetry.Tags.UserId, auditLog.UserId);
            }

            if (auditLog.EntityId is not null)
            {
                activity.SetTag(AITelemetry.Tags.EntityId, auditLog.EntityId);
            }

            if (auditLog.EntityType is not null)
            {
                activity.SetTag(AITelemetry.Tags.EntityType, auditLog.EntityType);
            }

            if (auditLog.FeatureType is not null)
            {
                activity.SetTag(AITelemetry.Tags.FeatureType, auditLog.FeatureType);
            }

            if (auditLog.FeatureId.HasValue)
            {
                activity.SetTag(AITelemetry.Tags.FeatureId, auditLog.FeatureId.Value.ToString());
            }

            return;
        }

        // Fallback: read directly from runtime context when audit logging is disabled
        var runtimeContext = _runtimeContextAccessor.Context;
        if (runtimeContext is null)
        {
            return;
        }

        var profileId = runtimeContext.GetValue<Guid>(Constants.ContextKeys.ProfileId);
        if (profileId != default)
        {
            activity.SetTag(AITelemetry.Tags.ProfileId, profileId.ToString());
        }

        var profileAlias = runtimeContext.GetValue<string>(Constants.ContextKeys.ProfileAlias);
        if (profileAlias is not null)
        {
            activity.SetTag(AITelemetry.Tags.ProfileAlias, profileAlias);
        }

        var entityId = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityId);
        if (entityId is not null)
        {
            activity.SetTag(AITelemetry.Tags.EntityId, entityId);
        }

        var entityType = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityType);
        if (entityType is not null)
        {
            activity.SetTag(AITelemetry.Tags.EntityType, entityType);
        }

        var featureType = runtimeContext.GetValue<string>(Constants.ContextKeys.FeatureType);
        if (featureType is not null)
        {
            activity.SetTag(AITelemetry.Tags.FeatureType, featureType);
        }

        var featureId = runtimeContext.GetValue<Guid>(Constants.ContextKeys.FeatureId);
        if (featureId != default)
        {
            activity.SetTag(AITelemetry.Tags.FeatureId, featureId.ToString());
        }
    }
}
