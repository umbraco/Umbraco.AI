namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Response model for a prompt.
/// </summary>
public class PromptResponseModel
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The unique alias.
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The prompt content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Optional linked profile ID.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IEnumerable<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether the prompt is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; init; }
}
