namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents extracted entity data ready for embedding.
/// </summary>
/// <param name="EntityKey">The unique entity key.</param>
/// <param name="EntityType">The entity type identifier (e.g., "content", "media", "member").</param>
/// <param name="EntitySubType">The entity sub-type (e.g., "article", "blogPost").</param>
/// <param name="Name">The entity name.</param>
/// <param name="Text">The extracted text content suitable for embedding.</param>
/// <param name="DateModified">The entity's last modification date.</param>
internal record SemanticIndexEntry(
    Guid EntityKey,
    string EntityType,
    string EntitySubType,
    string Name,
    string Text,
    DateTime DateModified);
