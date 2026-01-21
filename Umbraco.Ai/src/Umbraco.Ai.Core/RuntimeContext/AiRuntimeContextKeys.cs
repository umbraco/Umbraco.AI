namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Well-known keys for the runtime context data bag.
/// </summary>
public static class AiRuntimeContextKeys
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
    /// Key for the agent unique identifier (as Guid).
    /// </summary>
    public const string AgentId = "Umbraco.Ai.AgentId";
}
