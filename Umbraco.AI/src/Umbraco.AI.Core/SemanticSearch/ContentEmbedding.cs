namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents a stored embedding for a piece of Umbraco content or media.
/// </summary>
internal class ContentEmbedding
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Umbraco content/media key.
    /// </summary>
    public Guid ContentKey { get; set; }

    /// <summary>
    /// Gets or sets the type: "content" or "media".
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type alias (e.g., "article", "blogPost").
    /// </summary>
    public string ContentTypeAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content name at time of indexing.
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
    /// Gets or sets the content's last update date.
    /// </summary>
    public DateTime ContentDateModified { get; set; }
}
