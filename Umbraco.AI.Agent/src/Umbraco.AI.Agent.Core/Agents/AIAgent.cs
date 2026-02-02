using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Represents a stored agent definition that can be linked to AI profiles.
/// </summary>
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
    /// Profile to use for AI model configuration.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs assigned to this agent for AI context injection.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Scope IDs that categorize this agent for specific purposes.
    /// </summary>
    /// <remarks>
    /// Agents can belong to multiple scopes. An agent with no scopes will appear
    /// in general listings but not in any scoped queries.
    /// </remarks>
    public IReadOnlyList<string> ScopeIds { get; set; } = [];

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

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
