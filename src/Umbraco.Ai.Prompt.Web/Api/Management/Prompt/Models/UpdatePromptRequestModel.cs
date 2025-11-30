using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Request model for updating a prompt.
/// </summary>
public class UpdatePromptRequestModel
{
    /// <summary>
    /// The display name.
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// The prompt content.
    /// </summary>
    [Required]
    public required string Content { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; init; }

    /// <summary>
    /// Optional linked profile ID.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Optional tags for categorization.
    /// </summary>
    public IEnumerable<string>? Tags { get; init; }

    /// <summary>
    /// Whether the prompt is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
