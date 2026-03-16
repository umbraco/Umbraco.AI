namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Constants for configuring OpenTelemetry to capture Umbraco.AI telemetry.
/// </summary>
/// <example>
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(AITelemetry.SourceName))
///     .WithMetrics(m => m.AddMeter(AITelemetry.SourceName));
/// </code>
/// </example>
public static class AITelemetry
{
    /// <summary>
    /// The source name for all Umbraco.AI tracing and metrics.
    /// Use with <c>AddSource()</c> and <c>AddMeter()</c> in your OpenTelemetry configuration.
    /// </summary>
    public const string SourceName = "Umbraco.AI";

    /// <summary>
    /// Umbraco-specific tag names added to OpenTelemetry spans for enrichment.
    /// These tags appear on the <c>gen_ai.*</c> spans created by M.E.AI's OpenTelemetry middleware.
    /// </summary>
    public static class Tags
    {
        /// <summary>Tag for the AI profile ID.</summary>
        public const string ProfileId = "umbraco.ai.profile.id";

        /// <summary>Tag for the AI profile alias.</summary>
        public const string ProfileAlias = "umbraco.ai.profile.alias";

        /// <summary>Tag for the entity ID the AI operation targets.</summary>
        public const string EntityId = "umbraco.ai.entity.id";

        /// <summary>Tag for the entity type the AI operation targets.</summary>
        public const string EntityType = "umbraco.ai.entity.type";

        /// <summary>Tag for the feature type that initiated the operation (e.g., "prompt", "agent").</summary>
        public const string FeatureType = "umbraco.ai.feature.type";

        /// <summary>Tag for the feature ID that initiated the operation.</summary>
        public const string FeatureId = "umbraco.ai.feature.id";

        /// <summary>Tag for the audit log entry ID.</summary>
        public const string AuditId = "umbraco.ai.audit.id";

        /// <summary>Tag for the user ID who initiated the operation.</summary>
        public const string UserId = "umbraco.ai.user.id";
    }
}
