using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.ToolScope.Models;

/// <summary>
/// Lightweight response model for a tool scope (used in lists).
/// </summary>
public class ToolScopeItemResponseModel
{
    /// <summary>
    /// The unique identifier of the tool scope.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The icon identifier for the tool scope.
    /// </summary>
    [Required]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether tools in this scope are potentially destructive (write operations).
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// The domain this scope belongs to (e.g., "Content", "Media", "General").
    /// </summary>
    [Required]
    public string Domain { get; set; } = string.Empty;
}
