using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;

/// <summary>
/// Request model for updating an orchestration.
/// </summary>
public class UpdateOrchestrationRequestModel
{
    /// <summary>
    /// The unique alias (URL-safe identifier).
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Alias must contain only letters, numbers, hyphens, and underscores.")]
    public required string Alias { get; init; }

    /// <summary>
    /// The display name.
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; init; }

    /// <summary>
    /// Default profile for orchestration-level LLM calls.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Optional surface IDs that categorize this orchestration for specific purposes.
    /// </summary>
    public IEnumerable<string>? SurfaceIds { get; init; }

    /// <summary>
    /// Optional scope defining where this orchestration is available.
    /// If null, orchestration is available in all contexts.
    /// </summary>
    public AIAgentScopeModel? Scope { get; init; }

    /// <summary>
    /// The workflow graph definition containing nodes and edges.
    /// </summary>
    public OrchestrationGraphModel Graph { get; init; } = new();

    /// <summary>
    /// Whether the orchestration is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
