namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Defines where an agent is available, following the same pattern as Prompt scopes.
/// </summary>
/// <remarks>
/// <para>
/// If no allow rules are specified, the agent is available everywhere (backwards compatible).
/// If any allow rule matches, the agent is available (OR logic between rules).
/// Deny rules take precedence over allow rules.
/// </para>
/// <para>
/// This enables flexible context-based filtering:
/// <list type="bullet">
/// <item>Content-only agent: AllowRules with sectionAliases ["content"]</item>
/// <item>General agent (not in settings): DenyRules with sectionAliases ["settings"]</item>
/// <item>Document agent: AllowRules with sectionAliases ["content"], entityTypeAliases ["document"]</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AIAgentScope
{
    /// <summary>
    /// Rules that define where the agent is available.
    /// If any rule matches, the agent can be used (OR logic between rules).
    /// Empty means the agent is available everywhere (unless denied).
    /// </summary>
    public IReadOnlyList<AIAgentScopeRule> AllowRules { get; set; } = [];

    /// <summary>
    /// Rules that define where the agent is explicitly denied.
    /// If any rule matches, the agent cannot be used (OR logic between rules).
    /// Deny rules take precedence over allow rules.
    /// </summary>
    public IReadOnlyList<AIAgentScopeRule> DenyRules { get; set; } = [];
}
