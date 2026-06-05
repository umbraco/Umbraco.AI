namespace Umbraco.AI.Prompt.Core.Telemetry;

/// <summary>
/// Key names for the usage telemetry data Umbraco.AI.Prompt contributes to the CMS telemetry report.
/// </summary>
/// <remarks>
/// This is the complete safelist of data Umbraco.AI.Prompt reports. Values are always counts
/// or enum names — never prompt instructions, names, aliases, or user identities.
/// </remarks>
public static class AIPromptUsageTelemetryConstants
{
    /// <summary>The total number of prompts.</summary>
    public const string PromptCount = "UmbracoAIPromptCount";

    /// <summary>The number of active prompts.</summary>
    public const string PromptActiveCount = "UmbracoAIPromptActiveCount";

    /// <summary>The number of prompts linked to a specific profile (rather than the default).</summary>
    public const string PromptWithProfileCount = "UmbracoAIPromptWithProfileCount";

    /// <summary>The number of prompts with one or more contexts assigned.</summary>
    public const string PromptWithContextCount = "UmbracoAIPromptWithContextCount";

    /// <summary>The number of prompts with one or more guardrails assigned.</summary>
    public const string PromptWithGuardrailCount = "UmbracoAIPromptWithGuardrailCount";

    /// <summary>The set of display mode names in use across prompts.</summary>
    public const string PromptDisplayModes = "UmbracoAIPromptDisplayModes";

    /// <summary>The number of prompt executions in the last 30 days.</summary>
    public const string PromptExecutions30d = "UmbracoAIPromptExecutions30d";
}
