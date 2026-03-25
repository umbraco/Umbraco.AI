namespace Umbraco.AI.Search.SqlServer.VectorStore;

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
    /// Zero-based index of this chunk within the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// The vector stored as a SQL Server native vector type.
    /// </summary>
    public byte[] Vector { get; set; } = [];

    /// <summary>
    /// Optional JSON-serialized metadata associated with the vector.
    /// </summary>
    public string? Metadata { get; set; }
}
