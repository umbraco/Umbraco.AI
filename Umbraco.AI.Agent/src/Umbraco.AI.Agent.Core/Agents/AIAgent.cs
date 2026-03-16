using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Represents a stored agent definition that can be linked to AI profiles.
/// </summary>
/// <remarks>
/// <para>
/// Agents come in two types, determined by <see cref="AgentType"/>:
/// <list type="bullet">
///   <item><see cref="AIAgentType.Standard"/> — a standard agent with instructions, context injection, and tool permissions.</item>
///   <item><see cref="AIAgentType.Orchestrated"/> — an orchestrated agent that composes multiple agents into a workflow graph.</item>
/// </list>
/// </para>
/// <para>
/// Type-specific configuration is stored in <see cref="Config"/> and can be accessed via
/// <c>GetStandardConfig()</c> or <c>GetOrchestratedConfig()</c> extension methods.
/// </para>
/// </remarks>
public sealed class AIAgent : IAIVersionableEntity
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Unique alias for the agent (URL-safe identifier).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Display name for the agent.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what the agent does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of agent, determining its configuration shape and behavior.
    /// </summary>
    /// <remarks>
    /// Agent type is immutable after creation — it cannot be changed on update.
    /// </remarks>
    public AIAgentType AgentType { get; init; } = AIAgentType.Standard;

    /// <summary>
    /// Type-specific configuration for this agent.
    /// </summary>
    /// <remarks>
    /// The concrete type depends on <see cref="AgentType"/>:
    /// <list type="bullet">
    ///   <item><see cref="AIStandardAgentConfig"/> for <see cref="AIAgentType.Standard"/></item>
    ///   <item><see cref="AIOrchestratedAgentConfig"/> for <see cref="AIAgentType.Orchestrated"/></item>
    /// </list>
    /// </remarks>
    public IAIAgentConfig? Config { get; set; }

    /// <summary>
    /// Profile to use for AI model configuration.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Guardrail IDs assigned to this agent for safety and compliance checks.
    /// These guardrails are applied in addition to any profile-level guardrails during agent execution.
    /// For orchestrated agents, guardrails validate the initial input and final output.
    /// </summary>
    public IReadOnlyList<Guid> GuardrailIds { get; set; } = [];

    /// <summary>
    /// Surface IDs that categorize this agent for specific purposes.
    /// </summary>
    /// <remarks>
    /// Agents can belong to multiple surfaces. An agent with no surfaces will appear
    /// in general listings but not in any surface-specific queries.
    /// </remarks>
    public IReadOnlyList<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Optional scope defining where this agent is available.
    /// If null, agent is available in all contexts (backwards compatible).
    /// </summary>
    public AIAgentScope? Scope { get; set; }

    /// <summary>
    /// Whether this agent is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the agent was created.
    /// </summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the agent was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who created this agent.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this agent.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// The current version of the agent.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;
}
