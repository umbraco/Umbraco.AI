using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Context.Models;

/// <summary>
/// Lightweight response model for a context item (used in lists).
/// </summary>
public class ContextItemResponseModel
{
    /// <summary>
    /// The unique identifier of the context.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The alias of the context.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the context.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of resources in the context.
    /// </summary>
    public int ResourceCount { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the context was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the context was created.
    /// </summary>
    public DateTime DateModified { get; set; }
}
