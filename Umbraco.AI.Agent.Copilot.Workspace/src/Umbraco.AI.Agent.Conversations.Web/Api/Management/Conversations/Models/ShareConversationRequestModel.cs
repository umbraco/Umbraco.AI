namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API request model for sharing one of the acting user's own conversations with one or more
/// recipients.
/// </summary>
public sealed class ShareConversationRequestModel
{
    /// <summary>The backoffice users to share the conversation with.</summary>
    public IEnumerable<Guid> RecipientUserKeys { get; set; } = [];

    /// <summary>
    /// How much of the conversation stays visible to the recipients — <c>"Snapshot"</c> or
    /// <c>"Live"</c> (see <c>AIConversationShareScopeMode</c>), case-insensitive.
    /// </summary>
    public string ScopeMode { get; set; } = string.Empty;
}
