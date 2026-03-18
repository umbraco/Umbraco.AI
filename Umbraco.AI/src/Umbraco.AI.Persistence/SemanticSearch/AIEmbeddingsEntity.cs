namespace Umbraco.AI.Persistence.SemanticSearch;

/// <summary>
/// EF Core entity representing a stored embedding.
/// </summary>
internal class AIEmbeddingsEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The Umbraco content/media key.
    /// </summary>
    public Guid ContentKey { get; set; }

    /// <summary>
    /// The type: "content" or "media".
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The content type alias (e.g., "article", "blogPost").
    /// </summary>
    public string ContentTypeAlias { get; set; } = string.Empty;

    /// <summary>
    /// The content name at time of indexing.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The text content that was embedded.
    /// </summary>
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// The serialized embedding vector (float[] as byte[]).
    /// </summary>
    public byte[] Vector { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The vector dimensionality.
    /// </summary>
    public int Dimensions { get; set; }

    /// <summary>
    /// The embedding profile ID that generated this.
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The model identifier at time of embedding.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// When the embedding was generated.
    /// </summary>
    public DateTime DateIndexed { get; set; }

    /// <summary>
    /// The content's last update date.
    /// </summary>
    public DateTime ContentDateModified { get; set; }
}
