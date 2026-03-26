namespace Umbraco.AI.Search.Db.VectorStore;

/// <summary>
/// EF Core entity representing a stored vector with associated metadata.
/// </summary>
internal class AIVectorEntryEntity
{
    /// <summary>
    /// Auto-incrementing primary key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The name of the vector index this entry belongs to.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// The document identifier within the index.
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// The culture code for this entry, or null for invariant content.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Zero-based index of this chunk within the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// The vector stored as a JSON array (e.g. "[1.0, 2.0, 3.0]").
    /// </summary>
    public string Vector { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON-serialized metadata associated with the vector.
    /// </summary>
    public string? Metadata { get; set; }
}
