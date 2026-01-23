namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for a agent list item.
/// </summary>
public class AgentItemResponseModel
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
    /// The linked profile ID.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs for AI context injection.
    /// </summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; }
}
