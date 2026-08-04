namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API response model for a single persisted message in a conversation.
/// </summary>
public sealed class MessageResponseModel
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Server-assigned, contiguous position within the conversation.</summary>
    public int Sequence { get; set; }

    /// <summary>The message role (e.g. "user", "assistant", "tool").</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>The serialized message content (M.E.AI ChatMessage JSON).</summary>
    public string ContentJson { get; set; } = string.Empty;

    /// <summary>Plain-text projection of the content, when available.</summary>
    public string? ContentText { get; set; }

    /// <summary>Input token count for this message, when recorded.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Output token count for this message, when recorded.</summary>
    public int? OutputTokens { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }
}
