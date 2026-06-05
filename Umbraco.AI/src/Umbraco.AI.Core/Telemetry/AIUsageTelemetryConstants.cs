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

    /// <summary>The total number of contexts.</summary>
    public const string ContextCount = "UmbracoAIContextCount";

    /// <summary>The total number of guardrails.</summary>
    public const string GuardrailCount = "UmbracoAIGuardrailCount";

    /// <summary>
    /// The set of system-registered evaluator IDs in use across guardrail rules (evaluators
    /// shipped in Umbraco packages). Custom evaluators are counted in
    /// <see cref="GuardrailEvaluatorCustomCount"/>, never named.
    /// </summary>
    public const string GuardrailEvaluators = "UmbracoAIGuardrailEvaluators";

    /// <summary>The number of distinct custom (non-Umbraco.AI) evaluator IDs in use across guardrail rules.</summary>
    public const string GuardrailEvaluatorCustomCount = "UmbracoAIGuardrailEvaluatorCustomCount";

    /// <summary>The total number of tests defined.</summary>
    public const string TestCount = "UmbracoAITestCount";

    /// <summary>The total number of test runs executed.</summary>
    public const string TestRunCount = "UmbracoAITestRunCount";

    /// <summary>
    /// The set of system-registered test feature IDs in use across tests (features shipped
    /// in Umbraco packages). Custom features are counted in
    /// <see cref="TestFeatureCustomCount"/>, never named.
    /// </summary>
    public const string TestFeatures = "UmbracoAITestFeatures";

    /// <summary>The number of distinct custom (non-Umbraco.AI) test feature IDs in use across tests.</summary>
    public const string TestFeatureCustomCount = "UmbracoAITestFeatureCustomCount";

    /// <summary>
    /// The set of system-registered grader type IDs in use across tests (graders shipped
    /// in Umbraco packages). Custom graders are counted in
    /// <see cref="TestGraderCustomCount"/>, never named.
    /// </summary>
    public const string TestGraders = "UmbracoAITestGraders";

    /// <summary>The number of distinct custom (non-Umbraco.AI) grader type IDs in use across tests.</summary>
    public const string TestGraderCustomCount = "UmbracoAITestGraderCustomCount";

    /// <summary>The total number of registered AI tools. Tool IDs are never reported.</summary>
    public const string ToolCount = "UmbracoAIToolCount";

    /// <summary>The number of registered custom (non-Umbraco.AI) AI tools.</summary>
    public const string ToolCustomCount = "UmbracoAIToolCustomCount";

    /// <summary>The total number of registered context resource types.</summary>
    public const string ContextResourceTypeCount = "UmbracoAIContextResourceTypeCount";

    /// <summary>The number of registered custom (non-Umbraco.AI) context resource types.</summary>
    public const string ContextResourceTypeCustomCount = "UmbracoAIContextResourceTypeCustomCount";

    /// <summary>
    /// Builds the key for a per-pipeline custom (non-Umbraco.AI) middleware count, e.g.
    /// "UmbracoAIChatMiddlewareCustomCount". Pipelines are discovered from the
    /// <c>AI{Pipeline}MiddlewareCollection</c> types at runtime, so middleware for new
    /// capabilities is reported without changes here.
    /// </summary>
    public static string MiddlewareCustomCount(string pipeline) => $"UmbracoAI{pipeline}MiddlewareCustomCount";

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
