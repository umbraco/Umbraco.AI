namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// EF Core entity for a conversation's directly-attached resource. A column-for-column mirror of
/// <c>AIProjectResourceEntity</c>, keyed by <see cref="ConversationId"/> — the "attach a direct
/// resource" mechanism at conversation scope. <see cref="ResourceTypeId"/> is the pluggability seam
/// (an <c>[AIContextResourceType]</c>).
/// </summary>
internal class AIConversationResourceEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owning conversation id (FK, cascade delete).
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// The registered resource-type identifier.
    /// </summary>
    public string ResourceTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ordering within the conversation's resource list.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// JSON-serialized, schema-driven settings (sensitive fields encrypted by the serializer).
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Injection mode (Always / OnDemand), stored as its integer value.
    /// </summary>
    public int InjectionMode { get; set; }
}
