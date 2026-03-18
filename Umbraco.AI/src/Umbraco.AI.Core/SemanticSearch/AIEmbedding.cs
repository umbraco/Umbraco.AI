namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents a stored embedding for an Umbraco entity.
/// </summary>
internal class AIEmbedding
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Umbraco entity key.
    /// </summary>
    public Guid EntityKey { get; set; }

    /// <summary>
    /// Gets or sets the entity type (e.g., "content", "media").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type alias (e.g., "article", "blogPost", "Image").
    /// </summary>
    public string EntityTypeAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity name at time of indexing.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text that was embedded.
    /// </summary>
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized embedding vector (float[] as byte[]).
    /// </summary>
    public byte[] Vector { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the vector dimensionality.
    /// </summary>
    public int Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the embedding profile ID that generated this embedding.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier at time of embedding.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the embedding was generated.
    /// </summary>
    public DateTime DateIndexed { get; set; }

    /// <summary>
    /// Gets or sets the entity's last update date.
    /// </summary>
    public DateTime EntityDateModified { get; set; }
}
