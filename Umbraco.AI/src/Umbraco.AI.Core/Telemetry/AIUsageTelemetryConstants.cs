namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Key names for the usage telemetry data Umbraco.AI contributes to the CMS telemetry report.
/// </summary>
/// <remarks>
/// <para>
/// This is the complete whitelist of data Umbraco.AI core reports. Values are always counts,
/// booleans, or normalized identifiers — never user-authored content, names, aliases,
/// connection settings, token totals, or user identities. Unit tests assert that emitted
/// keys stay within this whitelist.
/// </para>
/// <para>
/// Not to be confused with <see cref="AITelemetry.Tags"/>, which are OpenTelemetry span tags
/// for the host application's own observability infrastructure.
/// </para>
/// </remarks>
public static class AIUsageTelemetryConstants
{
    /// <summary>The set of installed AI provider IDs (e.g., "openai", "anthropic").</summary>
    public const string Providers = "UmbracoAIProviders";

    /// <summary>The total number of configured connections.</summary>
    public const string ConnectionCount = "UmbracoAIConnectionCount";

    /// <summary>The set of provider IDs that have at least one connection configured.</summary>
    public const string ConnectedProviders = "UmbracoAIConnectedProviders";

    /// <summary>The total number of profiles.</summary>
    public const string ProfileCount = "UmbracoAIProfileCount";

    /// <summary>
    /// Prefix for per-capability profile counts. The <see cref="Models.AICapability"/> enum
    /// member name is appended (e.g., "UmbracoAIProfileCountChat").
    /// </summary>
    public const string ProfileCountPrefix = "UmbracoAIProfileCount";

    /// <summary>
    /// The set of normalized model families in use across profiles, in the form
    /// "{providerId}/{family}" (e.g., "openai/gpt-4o"). User-authored model or deployment
    /// names that don't match a known public model family are reported as "{providerId}/other".
    /// </summary>
    public const string ModelFamilies = "UmbracoAIModelFamilies";

    /// <summary>The total number of contexts.</summary>
    public const string ContextCount = "UmbracoAIContextCount";

    /// <summary>The total number of guardrails.</summary>
    public const string GuardrailCount = "UmbracoAIGuardrailCount";

    /// <summary>The set of capability names that have a default profile alias configured.</summary>
    public const string DefaultProfileCapabilities = "UmbracoAIDefaultProfileCapabilities";

    /// <summary>Whether audit logging is enabled.</summary>
    public const string AuditLogEnabled = "UmbracoAIAuditLogEnabled";

    /// <summary>Whether usage analytics is enabled.</summary>
    public const string AnalyticsEnabled = "UmbracoAIAnalyticsEnabled";

    /// <summary>The total number of AI requests in the last 30 days.</summary>
    public const string UsageRequests30d = "UmbracoAIUsageRequests30d";

    /// <summary>The request success rate (0.0–1.0) over the last 30 days.</summary>
    public const string UsageSuccessRate30d = "UmbracoAIUsageSuccessRate30d";
}
