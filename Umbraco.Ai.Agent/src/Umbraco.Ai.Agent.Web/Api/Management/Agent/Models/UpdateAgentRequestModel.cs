using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;

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
    /// </summary>
    [Required]
    public required Guid ProfileId { get; init; }

    /// <summary>
    /// Optional context IDs for AI context injection.
    /// </summary>
    public IEnumerable<Guid>? ContextIds { get; init; }

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
