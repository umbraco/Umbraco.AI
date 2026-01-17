namespace Umbraco.Ai.Persistence.Context;

/// <summary>
/// EF Core entity representing an AI context.
/// </summary>
public class AiContextEntity
{
    /// <summary>
    /// Unique identifier for the context.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias for the context (used for lookup).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the context.
    /// </summary>
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
    /// Navigation property to the context's resources.
    /// </summary>
    public ICollection<AiContextResourceEntity> Resources { get; set; } = [];
}
