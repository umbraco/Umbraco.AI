namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// A persisted message (domain model). Content is stored M.E.AI-agnostically as strings; the
/// <c>ConversationChatHistoryProvider</c> (Phase 3) performs the <c>ChatMessage</c> ↔ <see cref="AIMessage"/>
/// mapping.
/// </summary>
public sealed class AIMessage
{
    /// <summary>Unique identifier. Stable across loads/resumes for correlation.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning conversation id.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Ordering position within the conversation. Assigned authoritatively by the repository at store
    /// time (server-assigned sequence, interrogation B1) — any value set by a caller is ignored.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>Message role (e.g. "user", "assistant", "tool").</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Serialized M.E.AI <c>ChatMessage</c> JSON — the durable content record.</summary>
    public string ContentJson { get; set; } = string.Empty;

    /// <summary>Plain-text projection of the message for full-text history search (decision #2).</summary>
    public string? ContentText { get; set; }

    /// <summary>Version discriminator for the persisted <see cref="ContentJson"/> shape (interrogation C9).</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Optional input token count.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Optional output token count.</summary>
    public int? OutputTokens { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }
}
