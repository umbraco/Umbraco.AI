namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// User group-specific tool permission overrides for an agent.
/// User group ID is the dictionary key in the parent AgentResponseModel/UpdateAgentRequestModel.
/// </summary>
public class AIAgentUserGroupPermissionsModel
{
    /// <summary>
    /// Tool IDs explicitly allowed for this user group (additive).
    /// </summary>
    public IEnumerable<string> AllowedToolIds { get; set; } = [];

    /// <summary>
    /// Tool scope IDs allowed for this user group (additive).
    /// Tools matching these scopes will be included automatically.
    /// </summary>
    public IEnumerable<string> AllowedToolScopeIds { get; set; } = [];

    /// <summary>
    /// Tool IDs explicitly denied for this user group (subtractive).
    /// Takes precedence over agent defaults and allowed overrides.
    /// </summary>
    public IEnumerable<string> DeniedToolIds { get; set; } = [];

    /// <summary>
    /// Tool scope IDs denied for this user group (subtractive).
    /// Tools matching these scopes will be excluded.
    /// Takes precedence over agent defaults and allowed overrides.
    /// </summary>
    public IEnumerable<string> DeniedToolScopeIds { get; set; } = [];
}
