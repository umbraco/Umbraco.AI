namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Key names for the usage telemetry data Umbraco.AI contributes to the CMS telemetry report.
/// </summary>
/// <remarks>
/// <para>
/// This is the complete safelist of data Umbraco.AI core reports. Values are always counts,
/// booleans, or normalized identifiers — never user-authored content, names, aliases,
/// connection settings, token totals, or user identities. Unit tests assert that emitted
/// keys stay within this safelist.
/// </para>
/// <para>
/// Not to be confused with <see cref="AITelemetry.Tags"/>, which are OpenTelemetry span tags
/// for the host application's own observability infrastructure.
/// </para>
/// </remarks>
public static class AIUsageTelemetryConstants
{
    /// <summary>
    /// The set of installed system AI provider IDs (e.g., "openai", "anthropic" — providers
    /// shipped in Umbraco.AI packages). Custom providers are counted in
    /// <see cref="ProviderCustomCount"/>, never named.
    /// </summary>
    public const string Providers = "UmbracoAIProviders";

    /// <summary>The number of installed custom (non-Umbraco.AI) providers.</summary>
    public const string ProviderCustomCount = "UmbracoAIProviderCustomCount";

    /// <summary>The total number of configured connections.</summary>
    public const string ConnectionCount = "UmbracoAIConnectionCount";

    /// <summary>The set of system provider IDs that have at least one connection configured.</summary>
    public const string ConnectedProviders = "UmbracoAIConnectedProviders";

    /// <summary>The number of distinct custom (non-Umbraco.AI) provider IDs with at least one connection.</summary>
    public const string ConnectedProviderCustomCount = "UmbracoAIConnectedProviderCustomCount";

    /// <summary>The total number of profiles.</summary>
    public const string ProfileCount = "UmbracoAIProfileCount";

    /// <summary>
    /// Prefix for per-capability profile counts. The <see cref="Models.AICapability"/> enum
    /// member name is appended (e.g., "UmbracoAIProfileCountChat").
    /// </summary>
    public const string ProfileCountPrefix = "UmbracoAIProfileCount";

    /// <summary>The total number of contexts.</summary>
    public const string ContextCount = "UmbracoAIContextCount";

    /// <summary>The number of data types based on the AI Context Picker property editor.</summary>
    public const string ContextPickerDataTypeCount = "UmbracoAIContextPickerDataTypeCount";

    /// <summary>The number of content types referencing an AI Context Picker data type.</summary>
    public const string ContextPickerContentTypeCount = "UmbracoAIContextPickerContentTypeCount";

    /// <summary>Whether any content has saved AI Context Picker values (i.e. editors have actually assigned contexts to nodes).</summary>
    public const string ContextPickerHasSavedValues = "UmbracoAIContextPickerHasSavedValues";

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
    /// Builds the key for a per-extension-point custom (non-Umbraco.AI) registration count,
    /// e.g. "UmbracoAIChatMiddlewareCustomCount" or "UmbracoAIAgentWorkflowCustomCount".
    /// Extension points are discovered from the <c>AI{Name}Collection</c> types across loaded
    /// Umbraco.AI assemblies at runtime, so new extension points and capabilities are
    /// reported without changes here.
    /// </summary>
    public static string ExtensionCustomCount(string name) => $"UmbracoAI{name}CustomCount";

    /// <summary>The set of capability names that have a default profile alias configured.</summary>
    public const string DefaultProfileCapabilities = "UmbracoAIDefaultProfileCapabilities";

    /// <summary>
    /// The set of experimental feature names that are enabled (opted in via
    /// <c>Umbraco:AI:Experimental</c>), e.g. "ImageGeneration". Lets us track adoption of
    /// experimental capabilities while they remain gated. Discovered by reflection over the
    /// boolean flags on <see cref="Settings.AIExperimentalOptions"/>, so new flags are reported
    /// without changes here.
    /// </summary>
    public const string ExperimentalFeatures = "UmbracoAIExperimentalFeatures";

    /// <summary>Whether audit logging is enabled.</summary>
    public const string AuditLogEnabled = "UmbracoAIAuditLogEnabled";

    /// <summary>Whether usage analytics is enabled.</summary>
    public const string AnalyticsEnabled = "UmbracoAIAnalyticsEnabled";

    /// <summary>The total number of AI requests in the last 30 days.</summary>
    public const string UsageRequests30d = "UmbracoAIUsageRequests30d";

    /// <summary>
    /// Prefix for per-capability 30-day request counts. The <see cref="Models.AICapability"/>
    /// enum member name is appended (e.g., "UmbracoAIUsageRequests30dChat").
    /// </summary>
    public const string UsageRequests30dPrefix = "UmbracoAIUsageRequests30d";

    /// <summary>The request success rate (0.0–1.0) over the last 30 days.</summary>
    public const string UsageSuccessRate30d = "UmbracoAIUsageSuccessRate30d";
}
