using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Request model for creating a agent.
/// </summary>
public class CreateAgentRequestModel
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
    /// Optional context IDs for AI context injection.
    /// </summary>
    public IEnumerable<Guid>? ContextIds { get; init; }

    /// <summary>
    /// Optional scope IDs that categorize this agent for specific purposes.
    /// </summary>
    public IEnumerable<string>? ScopeIds { get; init; }

    /// <summary>
    /// Optional enabled tool IDs for this agent.
    /// Tools must be explicitly enabled or belong to an enabled scope.
    /// System tools are always enabled.
    /// </summary>
    public IEnumerable<string>? AllowedToolIds { get; init; }

    /// <summary>
    /// Optional enabled tool scope IDs for this agent.
    /// Tools belonging to these scopes are automatically enabled.
    /// </summary>
    public IEnumerable<string>? AllowedToolScopeIds { get; init; }

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; init; }
}
