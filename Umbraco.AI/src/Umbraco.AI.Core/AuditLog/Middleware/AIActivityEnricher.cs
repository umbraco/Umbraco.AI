using System.Diagnostics;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Core.AuditLog.Middleware;

/// <summary>
/// Enriches the ambient <see cref="Activity"/> with Umbraco-specific tags.
/// Tags are added to the span created by M.E.AI's OpenTelemetry middleware.
/// This is a no-op when no Activity is active (i.e., OpenTelemetry is not configured).
/// </summary>
internal static class AIActivityEnricher
{
    /// <summary>
    /// Enriches <see cref="Activity.Current"/> with Umbraco AI context tags.
    /// When an audit log exists, uses its pre-extracted values. Otherwise falls back
    /// to reading directly from the runtime context (for when audit logging is disabled).
    /// </summary>
    public static void EnrichCurrentActivity(AIAuditLog? auditLog, IAIRuntimeContextAccessor runtimeContextAccessor)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        if (auditLog is not null)
        {
            EnrichFromAuditLog(activity, auditLog);
        }
        else
        {
            EnrichFromRuntimeContext(activity, runtimeContextAccessor.Context);
        }
    }

    private static void EnrichFromAuditLog(Activity activity, AIAuditLog auditLog)
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
    }

    private static void EnrichFromRuntimeContext(Activity activity, AIRuntimeContext? runtimeContext)
    {
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
