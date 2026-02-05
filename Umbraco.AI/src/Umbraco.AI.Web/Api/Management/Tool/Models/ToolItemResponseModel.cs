using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Tool.Models;

/// <summary>
/// Lightweight response model for a tool (used in lists and pickers).
/// </summary>
public class ToolItemResponseModel
{
    /// <summary>
    /// The unique identifier of the tool.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the tool.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of what the tool does.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The scope identifier for permission and grouping purposes.
    /// </summary>
    [Required]
    public string ScopeId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the tool performs destructive operations.
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// Tags for additional categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
}
