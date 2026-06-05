namespace Umbraco.AI.Agent.Core.Telemetry;

/// <summary>
/// Key names for the usage telemetry data Umbraco.AI.Agent contributes to the CMS telemetry report.
/// </summary>
/// <remarks>
/// This is the complete whitelist of data Umbraco.AI.Agent reports. Values are always counts,
/// enum names, or code-authored surface IDs — never agent instructions, names, aliases, or
/// user identities.
/// </remarks>
public static class AIAgentUsageTelemetryConstants
{
    /// <summary>The total number of agents.</summary>
    public const string AgentCount = "UmbracoAIAgentCount";

    /// <summary>The number of active agents.</summary>
    public const string AgentActiveCount = "UmbracoAIAgentActiveCount";

    /// <summary>
    /// Prefix for per-type agent counts. The <see cref="Agents.AIAgentType"/> enum member name
    /// is appended (e.g., "UmbracoAIAgentCountStandard").
    /// </summary>
    public const string AgentCountPrefix = "UmbracoAIAgentCount";

    /// <summary>The number of agents linked to a specific profile (rather than the default).</summary>
    public const string AgentWithProfileCount = "UmbracoAIAgentWithProfileCount";

    /// <summary>The number of agents with one or more guardrails assigned.</summary>
    public const string AgentWithGuardrailCount = "UmbracoAIAgentWithGuardrailCount";

    /// <summary>
    /// The set of system-registered surface IDs in use across agents (surfaces shipped in
    /// Umbraco packages). Custom surfaces are counted in
    /// <see cref="AgentSurfaceCustomCount"/>, never named.
    /// </summary>
    public const string AgentSurfaces = "UmbracoAIAgentSurfaces";

    /// <summary>The number of distinct custom (non-Umbraco.AI) surface IDs in use across agents.</summary>
    public const string AgentSurfaceCustomCount = "UmbracoAIAgentSurfaceCustomCount";
}
