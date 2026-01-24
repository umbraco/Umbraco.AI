using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Context.Models;

/// <summary>
/// Full response model for a context (includes resources).
/// </summary>
public class ContextResponseModel
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
    /// Date and time when the context was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Date and time when the context was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The resources belonging to this context.
    /// </summary>
    public IReadOnlyList<ContextResourceModel> Resources { get; set; } = [];

    /// <summary>
    /// The current version number of the entity.
    /// </summary>
    public int Version { get; set; }
}
