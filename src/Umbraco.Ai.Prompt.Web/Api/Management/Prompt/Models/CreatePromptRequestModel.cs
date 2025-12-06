using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Request model for creating a prompt.
/// </summary>
public class CreatePromptRequestModel
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
    /// Optional visibility configuration defining where this prompt appears.
    /// Null means the prompt does not appear anywhere.
    /// </summary>
    public VisibilityModel? Visibility { get; init; }
}
