using System.Text.Json.Serialization;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// Session-scoped state persisted in the MAF <c>AgentSession</c> for the
/// <see cref="ConversationChatHistoryProvider"/> — carries the bound conversation id so the (shared)
/// provider instance holds no session-specific state in its own fields.
/// </summary>
internal sealed class ConversationSessionState
{
    /// <summary>The persisted conversation this session is bound to.</summary>
    [JsonPropertyName("conversationId")]
    public Guid ConversationId { get; set; }
}
