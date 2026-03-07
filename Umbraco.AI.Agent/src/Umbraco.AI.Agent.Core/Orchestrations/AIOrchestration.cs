using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Represents a stored orchestration definition that composes multiple agents into a workflow.
/// </summary>
/// <remarks>
/// <para>
/// Orchestrations use Microsoft Agent Framework (MAF) workflows to coordinate multiple agents.
/// The workflow is defined as a directed graph of nodes and edges, stored as JSON.
/// </para>
/// <para>
/// Orchestrations can be exposed as agents via surfaces (e.g., "copilot") — at runtime,
/// the graph is translated into a MAF <c>Workflow</c> and wrapped as an agent.
/// </para>
/// </remarks>
public sealed class AIOrchestration : IAIVersionableEntity
{
    /// <summary>
    /// Unique identifier for the orchestration.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Unique alias for the orchestration (URL-safe identifier).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Display name for the orchestration.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what the orchestration does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default profile for orchestration-level LLM calls (e.g., aggregation summarization, manager delegation).
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Surface IDs that categorize this orchestration for specific purposes.
    /// </summary>
    /// <remarks>
    /// Orchestrations can belong to multiple surfaces. When assigned to a surface,
    /// the orchestration appears alongside regular agents in that surface's UI.
    /// </remarks>
    public IReadOnlyList<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Optional scope defining where this orchestration is available.
    /// If null, orchestration is available in all contexts.
    /// </summary>
    /// <remarks>
    /// Follows the same pattern as <see cref="AIAgentScope"/> with allow and deny rules.
    /// </remarks>
    public AIAgentScope? Scope { get; set; }

    /// <summary>
    /// The workflow graph definition containing nodes and edges.
    /// </summary>
    public AIOrchestrationGraph Graph { get; set; } = new();

    /// <summary>
    /// Whether this orchestration is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the orchestration was created.
    /// </summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the orchestration was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who created this orchestration.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this orchestration.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// The current version of the orchestration.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;
}
