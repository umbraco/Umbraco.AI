using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Represents a stored prompt template that can be linked to AI profiles.
/// </summary>
public sealed class AIPrompt : IAIVersionableEntity
{
    /// <summary>
    /// Unique identifier for the prompt.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Unique alias for the prompt (URL-safe identifier).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Display name for the prompt.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what the prompt does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The prompt instructions template. May include placeholders like {{variable}}.
    /// </summary>
    public required string Instructions { get; set; }

    /// <summary>
    /// Optional ID of the AI profile this prompt is designed for.
    /// References AIProfile.Id from Umbraco.AI.Core.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs assigned to this prompt for AI context injection.
    /// These contexts provide brand voice, guidelines, and reference materials for AI operations.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Tags for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether this prompt is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether to include the full entity context as a system message during prompt execution.
    /// When true, all entity properties are formatted as markdown and injected.
    /// Variable replacement ({{property}}) works regardless of this setting.
    /// </summary>
    public bool IncludeEntityContext { get; set; } = true;

    /// <summary>
    /// Scope configuration defining where this prompt can run.
    /// Controls both UI display and server-side enforcement.
    /// If null, the prompt is not allowed anywhere (denied by default).
    /// </summary>
    public AIPromptScope? Scope { get; set; }

    /// <summary>
    /// When the prompt was created.
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// When the prompt was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The key (GUID) of the user who created this prompt.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this prompt.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// The current version of the prompt.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;
}
