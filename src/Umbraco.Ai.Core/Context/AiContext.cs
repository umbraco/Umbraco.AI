namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Represents an AI context containing resources that enrich AI operations with brand voice,
/// guidelines, and reference materials.
/// </summary>
/// <remarks>
/// Contexts are standalone, reusable entities that can be assigned to content nodes (via property editor),
/// profiles, prompts, and agents. They are not owned by these entities but referenced by them.
/// </remarks>
public sealed class AiContext
{
    /// <summary>
    /// The unique identifier of the AI context.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The immutable alias of the AI context (e.g., "corporate-brand-voice").
    /// Used for programmatic reference.
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The display name of the AI context (e.g., "Corporate Brand Voice").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The date and time when the context was created.
    /// </summary>
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the context was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The resources belonging to this context.
    /// Resources are ordered by <see cref="AiContextResource.SortOrder"/>.
    /// </summary>
    public IList<AiContextResource> Resources { get; set; } = [];
}
