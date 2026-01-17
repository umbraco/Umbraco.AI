using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Context.Models;

/// <summary>
/// Request model for creating a new context.
/// </summary>
public class CreateContextRequestModel
{
    /// <summary>
    /// The alias of the context (immutable after creation).
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the context.
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The resources to create with this context.
    /// </summary>
    public IReadOnlyList<ContextResourceModel> Resources { get; set; } = [];
}
