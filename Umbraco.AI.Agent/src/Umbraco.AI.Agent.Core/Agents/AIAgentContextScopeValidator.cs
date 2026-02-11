namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Validates agent context scope rules, following the same pattern as Prompt scope validation.
/// </summary>
public class AIAgentContextScopeValidator
{
    /// <summary>
    /// Checks if an agent is available in the given context.
    /// </summary>
    /// <param name="agent">The agent to check.</param>
    /// <param name="context">The current context.</param>
    /// <returns>True if agent is available, false otherwise.</returns>
    public bool IsAgentAvailable(AIAgent agent, AgentAvailabilityContext context)
    {
        // No context scope = available everywhere (backwards compatible)
        if (agent.ContextScope == null)
        {
            return true;
        }

        // Check deny rules first (they take precedence)
        if (IsAnyRuleMatched(agent.ContextScope.DenyRules, context))
        {
            return false;
        }

        // No allow rules = available everywhere (unless denied above)
        if (agent.ContextScope.AllowRules.Count == 0)
        {
            return true;
        }

        // Check if any allow rule matches
        return IsAnyRuleMatched(agent.ContextScope.AllowRules, context);
    }

    /// <summary>
    /// Checks if any rule in the list matches the current context.
    /// OR logic between rules.
    /// </summary>
    private bool IsAnyRuleMatched(
        IReadOnlyList<AIAgentContextScopeRule> rules,
        AgentAvailabilityContext context)
    {
        // No rules = no match
        if (rules.Count == 0)
        {
            return false;
        }

        // Check if any rule matches (OR logic)
        return rules.Any(rule => IsRuleMatched(rule, context));
    }

    /// <summary>
    /// Checks if a single rule matches the current context.
    /// AND logic between properties, OR logic within arrays.
    /// </summary>
    private bool IsRuleMatched(
        AIAgentContextScopeRule rule,
        AgentAvailabilityContext context)
    {
        // Check section (if specified)
        if (rule.SectionAliases?.Count > 0)
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

        // Check entity type (if specified)
        if (rule.EntityTypeAliases?.Count > 0)
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

        // Check workspace (if specified)
        if (rule.WorkspaceAliases?.Count > 0)
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

        // All specified constraints satisfied (AND logic)
        return true;
    }
}
