namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Represents user group-specific tool permission overrides for an agent.
/// User group ID is the dictionary key, not stored in this model.
/// </summary>
public sealed class AIAgentUserGroupPermissions
{
    /// <summary>
    /// Tool IDs explicitly allowed for this user group (additive).
    /// </summary>
    public IReadOnlyList<string> AllowedToolIds { get; set; } = [];

    /// <summary>
    /// Tool scope IDs allowed for this user group (additive).
    /// Tools matching these scopes will be included automatically.
    /// </summary>
    public IReadOnlyList<string> AllowedToolScopeIds { get; set; } = [];

    /// <summary>
    /// Tool IDs explicitly denied for this user group (subtractive).
    /// Takes precedence over agent defaults and allowed overrides.
    /// </summary>
    public IReadOnlyList<string> DeniedToolIds { get; set; } = [];

    /// <summary>
    /// Tool scope IDs denied for this user group (subtractive).
    /// Tools matching these scopes will be excluded.
    /// Takes precedence over agent defaults and allowed overrides.
    /// </summary>
    public IReadOnlyList<string> DeniedToolScopeIds { get; set; } = [];
}
