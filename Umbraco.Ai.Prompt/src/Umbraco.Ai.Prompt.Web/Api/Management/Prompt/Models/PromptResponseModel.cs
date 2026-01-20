namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Response model for a prompt.
/// </summary>
public class PromptResponseModel
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique alias.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The prompt instructions.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Optional linked profile ID.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs for AI context injection.
    /// </summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether the prompt is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Scope configuration defining where this prompt can run.
    /// Null means the prompt is not allowed anywhere.
    /// </summary>
    public ScopeModel? Scope { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }
}
