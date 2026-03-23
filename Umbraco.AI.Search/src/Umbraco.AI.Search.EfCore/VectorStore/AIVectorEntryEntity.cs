namespace Umbraco.AI.Search.EfCore.VectorStore;

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
    /// The vector stored as a binary blob (IEEE 754 float array).
    /// </summary>
    public byte[] Vector { get; set; } = [];

    /// <summary>
    /// Optional JSON-serialized metadata associated with the vector.
    /// </summary>
    public string? Metadata { get; set; }
}
