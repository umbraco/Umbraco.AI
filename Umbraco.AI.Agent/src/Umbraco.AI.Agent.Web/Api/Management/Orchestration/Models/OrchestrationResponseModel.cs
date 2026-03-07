using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;

/// <summary>
/// Response model for an orchestration.
/// </summary>
public class OrchestrationResponseModel
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
    /// Default profile for orchestration-level LLM calls.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Surface IDs that categorize this orchestration for specific purposes.
    /// </summary>
    public IEnumerable<string> SurfaceIds { get; set; } = [];

    /// <summary>
    /// Optional scope defining where this orchestration is available.
    /// If null, orchestration is available in all contexts.
    /// </summary>
    public AIAgentScopeModel? Scope { get; set; }

    /// <summary>
    /// The workflow graph definition containing nodes and edges.
    /// </summary>
    public OrchestrationGraphModel Graph { get; set; } = new();

    /// <summary>
    /// Whether the orchestration is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the orchestration was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the orchestration was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The current version number of the orchestration.
    /// </summary>
    public int Version { get; set; }
}
