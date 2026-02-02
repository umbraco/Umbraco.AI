using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Context.Models;

/// <summary>
/// Model representing a resource within a context.
/// </summary>
public class ContextResourceModel
{
    /// <summary>
    /// The unique identifier of the resource.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The resource type identifier (e.g., "brand-voice", "text").
    /// </summary>
    [Required]
    public string ResourceTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the resource.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the resource.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sort order within the context.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Type-specific data object.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Injection mode (Always, OnDemand).
    /// </summary>
    [Required]
    public string InjectionMode { get; set; } = "Always";
}
