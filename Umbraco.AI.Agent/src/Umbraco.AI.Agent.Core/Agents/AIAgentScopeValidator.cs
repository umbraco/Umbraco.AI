using Umbraco.AI.Agent.Core.Surfaces;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Validates agent scope rules, following the same pattern as Prompt scope validation.
/// </summary>
/// <remarks>
/// <para>
/// This validator is surface-aware: it only checks context dimensions that the requesting
/// surface declares it cares about via <see cref="IAIAgentSurface.SupportedContextDimensions"/>.
/// </para>
/// <para>
/// For example, if a Copilot surface only checks ["section", "entityType"], then workspace
/// rules in the agent's scope will be ignored when filtering for that surface.
/// </para>
/// </remarks>
public class AIAgentScopeValidator
{
    /// <summary>
    /// Checks if an agent is available in the given context for a specific surface.
    /// </summary>
    /// <param name="agent">The agent to check.</param>
    /// <param name="context">The current context.</param>
    /// <param name="surface">The surface requesting the agent (determines which dimensions to check).</param>
    /// <returns>True if agent is available, false otherwise.</returns>
    public bool IsAgentAvailable(AIAgent agent, AgentAvailabilityContext context, IAIAgentSurface? surface)
    {
        // No scope = available everywhere (backwards compatible)
        if (agent.Scope == null)
        {
            return true;
        }

        // Get the dimensions this surface cares about
        var relevantDimensions = surface?.SupportedContextDimensions ?? Array.Empty<string>();

        // Check deny rules first (they take precedence)
        if (IsAnyRuleMatched(agent.Scope.DenyRules, context, relevantDimensions))
        {
            return false;
        }

        // No allow rules = available everywhere (unless denied above)
        if (agent.Scope.AllowRules.Count == 0)
        {
            return true;
        }

        // Check if any allow rule matches
        return IsAnyRuleMatched(agent.Scope.AllowRules, context, relevantDimensions);
    }

    /// <summary>
    /// Checks if any rule in the list matches the current context.
    /// OR logic between rules.
    /// </summary>
    /// <param name="rules">The rules to check.</param>
    /// <param name="context">The current context.</param>
    /// <param name="relevantDimensions">The dimensions the requesting surface cares about.</param>
    private bool IsAnyRuleMatched(
        IReadOnlyList<AIAgentScopeRule> rules,
        AgentAvailabilityContext context,
        IReadOnlyList<string> relevantDimensions)
    {
        // No rules = no match
        if (rules.Count == 0)
        {
            return false;
        }

        // Check if any rule matches (OR logic)
        return rules.Any(rule => IsRuleMatched(rule, context, relevantDimensions));
    }

    /// <summary>
    /// Checks if a single rule matches the current context.
    /// AND logic between properties, OR logic within arrays.
    /// Only checks dimensions that the surface cares about.
    /// </summary>
    /// <param name="rule">The rule to check.</param>
    /// <param name="context">The current context.</param>
    /// <param name="relevantDimensions">The dimensions the requesting surface cares about.</param>
    private bool IsRuleMatched(
        AIAgentScopeRule rule,
        AgentAvailabilityContext context,
        IReadOnlyList<string> relevantDimensions)
    {
        // Check section (if specified AND surface cares about it)
        if (rule.SectionAliases?.Count > 0 &&
            relevantDimensions.Contains("section", StringComparer.OrdinalIgnoreCase))
        {
            // No current section = doesn't match
            if (string.IsNullOrEmpty(context.SectionAlias))
            {
                return false;
            }

            // Check if current section is in the list (OR logic)
            if (!rule.SectionAliases.Contains(context.SectionAlias, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check entity type (if specified AND surface cares about it)
        if (rule.EntityTypeAliases?.Count > 0 &&
            relevantDimensions.Contains("entityType", StringComparer.OrdinalIgnoreCase))
        {
            // No current entity type = doesn't match
            if (string.IsNullOrEmpty(context.EntityTypeAlias))
            {
                return false;
            }

            // Check if current entity type is in the list (OR logic)
            if (!rule.EntityTypeAliases.Contains(context.EntityTypeAlias, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check workspace (if specified AND surface cares about it)
        if (rule.WorkspaceAliases?.Count > 0 &&
            relevantDimensions.Contains("workspace", StringComparer.OrdinalIgnoreCase))
        {
            // No current workspace = doesn't match
            if (string.IsNullOrEmpty(context.WorkspaceAlias))
            {
                return false;
            }

            // Check if current workspace is in the list (OR logic)
            if (!rule.WorkspaceAliases.Contains(context.WorkspaceAlias, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // All relevant specified constraints satisfied (AND logic)
        return true;
    }
}
