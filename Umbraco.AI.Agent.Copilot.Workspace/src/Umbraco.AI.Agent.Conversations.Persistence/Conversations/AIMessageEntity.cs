namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// EF Core entity for a single persisted message. Hybrid storage: relational metadata plus a
/// serialized M.E.AI <c>ChatMessage</c> JSON blob in <see cref="ContentJson"/>.
/// </summary>
internal class AIMessageEntity
{
    /// <summary>
    /// Unique identifier. Stable across loads/resumes to correlate persisted messages.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owning conversation id (FK, cascade delete).
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Ordering position within the conversation. Server-assigned; unique per conversation
    /// (interrogation B1). The ordering anchor for paging.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Message role (e.g. "user", "assistant", "tool").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Serialized M.E.AI <c>ChatMessage</c> (via <c>AIJsonUtilities.DefaultOptions</c>) — the durable
    /// content record (text, attachments, reasoning, tool calls/results, approvals).
    /// </summary>
    public string ContentJson { get; set; } = string.Empty;

    /// <summary>
    /// Denormalized plain-text projection of the message, populated at write time to back full-text
    /// history search (open decision #2 — content search).
    /// </summary>
    public string? ContentText { get; set; }

    /// <summary>
    /// Version discriminator for the persisted <see cref="ContentJson"/> shape, so the on-disk M.E.AI
    /// format has a migration handle across library-major upgrades (interrogation C9).
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Optional input token count (may be null when only aggregate usage is available).
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Optional output token count (may be null when only aggregate usage is available).
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }
}
