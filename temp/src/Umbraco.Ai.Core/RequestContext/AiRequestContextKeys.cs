namespace Umbraco.Ai.Core.RequestContext;

/// <summary>
/// Well-known keys for the request context data bag.
/// </summary>
public static class AiRequestContextKeys
{
    /// <summary>
    /// Key for <see cref="EntityAdapter.AiSerializedEntity"/> data.
    /// </summary>
    public const string SerializedEntity = "Umbraco.Ai.SerializedEntity";

    /// <summary>
    /// Key for user text selection data.
    /// </summary>
    public const string UserSelection = "Umbraco.Ai.UserSelection";

    /// <summary>
    /// Key for the entity unique identifier (as Guid).
    /// </summary>
    public const string EntityId = "Umbraco.Ai.EntityId";

    /// <summary>
    /// Key for the parent entity unique identifier (as Guid).
    /// Set when creating a new entity under a parent.
    /// </summary>
    public const string ParentEntityId = "Umbraco.Ai.ParentEntityId";

    /// <summary>
    /// Key for the entity type string.
    /// </summary>
    public const string EntityType = "Umbraco.Ai.EntityType";

    /// <summary>
    /// Key for content ID in ChatOptions.AdditionalProperties.
    /// Used by <c>ContentContextResolver</c> to resolve content contexts.
    /// </summary>
    public const string ContentId = "Umbraco.Ai.ContentId";
}
