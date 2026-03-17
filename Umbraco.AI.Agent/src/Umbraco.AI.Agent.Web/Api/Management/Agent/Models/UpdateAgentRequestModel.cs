using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Request model for updating a agent.
/// </summary>
public class UpdateAgentRequestModel
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
    /// The linked profile ID.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Optional guardrail IDs for safety and compliance checks.
    /// </summary>
    public IEnumerable<Guid>? GuardrailIds { get; init; }

    /// <summary>
    /// Optional surface IDs that categorize this agent for specific purposes.
    /// </summary>
    public IEnumerable<string>? SurfaceIds { get; init; }

    /// <summary>
    /// Optional scope defining where this agent is available.
    /// If null, agent is available in all contexts (backwards compatible).
    /// </summary>
    public AIAgentScopeModel? Scope { get; init; }

    /// <summary>
    /// Type-specific configuration for this agent.
    /// </summary>
    public AgentConfigModel? Config { get; init; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
