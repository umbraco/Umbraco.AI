using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Context.Models;

/// <summary>
/// Request model for updating an existing context.
/// </summary>
public class UpdateContextRequestModel
{
    /// <summary>
    /// The alias of the context (can be updated).
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
    /// The resources for this context (replaces existing resources).
    /// </summary>
    public IReadOnlyList<ContextResourceModel> Resources { get; set; } = [];
}
