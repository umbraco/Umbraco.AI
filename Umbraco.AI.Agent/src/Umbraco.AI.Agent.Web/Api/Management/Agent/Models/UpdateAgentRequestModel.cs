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
    /// Optional context IDs for AI context injection.
    /// </summary>
    public IEnumerable<Guid>? ContextIds { get; init; }

    /// <summary>
    /// Optional surface IDs that categorize this agent for specific purposes.
    /// </summary>
    public IEnumerable<string>? SurfaceIds { get; init; }

    /// <summary>
    /// Optional context scope defining where this agent is available.
    /// If null, agent is available in all contexts (backwards compatible).
    /// </summary>
    public AIAgentContextScopeModel? ContextScope { get; init; }

    /// <summary>
    /// Optional allowed tool IDs for this agent.
    /// Tools must be explicitly allowed or belong to an allowed scope.
    /// System tools are always allowed.
    /// </summary>
    public IEnumerable<string>? AllowedToolIds { get; init; }

    /// <summary>
    /// Optional allowed tool scope IDs for this agent.
    /// Tools belonging to these scopes are automatically allowed.
    /// </summary>
    public IEnumerable<string>? AllowedToolScopeIds { get; init; }

    /// <summary>
    /// User group-specific permission overrides.
    /// Dictionary key is UserGroupId (Guid).
    /// </summary>
    public Dictionary<Guid, AIAgentUserGroupPermissionsModel>? UserGroupPermissions { get; init; }

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
