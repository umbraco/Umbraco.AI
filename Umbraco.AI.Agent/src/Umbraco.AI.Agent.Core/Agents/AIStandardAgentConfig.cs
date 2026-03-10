namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Configuration for a standard agent with instructions, context injection, and tool permissions.
/// </summary>
public sealed class AIStandardAgentConfig : IAIAgentConfig
{
    /// <summary>
    /// Context IDs assigned to this agent for AI context injection.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Tool IDs explicitly allowed for this agent.
    /// Empty list means no specific tools are allowed (only scopes apply).
    /// </summary>
    public IReadOnlyList<string> AllowedToolIds { get; set; } = [];

    /// <summary>
    /// Tool scopes allowed for this agent.
    /// Tools matching these scopes will be included automatically.
    /// </summary>
    public IReadOnlyList<string> AllowedToolScopeIds { get; set; } = [];

    /// <summary>
    /// User group-specific permission overrides.
    /// Dictionary key is UserGroupId (Guid).
    /// </summary>
    public IReadOnlyDictionary<Guid, AIAgentUserGroupPermissions> UserGroupPermissions { get; set; }
        = new Dictionary<Guid, AIAgentUserGroupPermissions>();
}
