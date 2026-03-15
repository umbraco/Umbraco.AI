using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Polymorphic base class for agent type-specific configuration.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(StandardAgentConfigModel), "standard")]
[JsonDerivedType(typeof(OrchestratedAgentConfigModel), "orchestrated")]
public abstract class AgentConfigModel { }

/// <summary>
/// Configuration model for a standard agent with instructions, context injection, and tool permissions.
/// </summary>
public sealed class StandardAgentConfigModel : AgentConfigModel
{
    /// <summary>
    /// Optional context IDs for AI context injection.
    /// </summary>
    public List<Guid>? ContextIds { get; set; }

    /// <summary>
    /// Optional guardrail IDs for safety and compliance checks.
    /// </summary>
    public List<Guid>? GuardrailIds { get; set; }

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Optional allowed tool IDs for this agent.
    /// Tools must be explicitly allowed or belong to an allowed scope.
    /// System tools are always allowed.
    /// </summary>
    public List<string>? AllowedToolIds { get; set; }

    /// <summary>
    /// Optional allowed tool scope IDs for this agent.
    /// Tools belonging to these scopes are automatically allowed.
    /// </summary>
    public List<string>? AllowedToolScopeIds { get; set; }

    /// <summary>
    /// User group-specific permission overrides.
    /// Dictionary key is UserGroupId (Guid).
    /// </summary>
    public Dictionary<Guid, AIAgentUserGroupPermissionsModel>? UserGroupPermissions { get; set; }
}

/// <summary>
/// Configuration model for an orchestrated agent that uses a registered workflow.
/// </summary>
public sealed class OrchestratedAgentConfigModel : AgentConfigModel
{
    /// <summary>
    /// The ID of the registered workflow to use.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Workflow-specific settings as structured JSON.
    /// </summary>
    public JsonElement? Settings { get; set; }
}
