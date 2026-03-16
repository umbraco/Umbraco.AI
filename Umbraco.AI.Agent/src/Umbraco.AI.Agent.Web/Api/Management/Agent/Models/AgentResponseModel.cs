namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for a agent.
/// </summary>
public class AgentResponseModel
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique alias.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of agent ("standard" or "orchestrated").
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// The linked profile ID.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Guardrail IDs for safety and compliance checks.
    /// </summary>
    public IEnumerable<Guid> GuardrailIds { get; set; } = [];

    /// <summary>
    /// Surface IDs that categorize this agent for specific purposes.
    /// </summary>
    public IEnumerable<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Optional scope defining where this agent is available.
    /// If null, agent is available in all contexts (backwards compatible).
    /// </summary>
    public AIAgentScopeModel? Scope { get; set; }

    /// <summary>
    /// Type-specific configuration for this agent.
    /// </summary>
    public AgentConfigModel? Config { get; set; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the context was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the context was created.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The current version number of the entity.
    /// </summary>
    public int Version { get; set; }
}
