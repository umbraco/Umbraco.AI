namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Represents extracted entity data ready for embedding.
/// </summary>
/// <param name="EntityKey">The unique entity key.</param>
/// <param name="EntityType">The entity type identifier (e.g., "content", "media", "member").</param>
/// <param name="EntityTypeAlias">The entity type alias (e.g., "article", "blogPost").</param>
/// <param name="Name">The entity name.</param>
/// <param name="Text">The extracted text content suitable for embedding.</param>
/// <param name="DateModified">The entity's last modification date.</param>
internal record SemanticIndexEntry(
    Guid EntityKey,
    string EntityType,
    string EntityTypeAlias,
    string Name,
    string Text,
    DateTime DateModified);
