namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API response model for one row of the acting user's "shared with me" list
/// (<c>GET /conversations/shared-with-me</c>). No message content — that's only returned by
/// <c>GET /conversations/{id}/shared-view</c>.
/// </summary>
public sealed class SharedConversationSummaryResponseModel
{
    /// <summary>The shared conversation's id.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>The shared conversation's display title.</summary>
    public string? Title { get; set; }

    /// <summary>The conversation owner's display name, resolved at request time.</summary>
    public string OwnerDisplayName { get; set; } = string.Empty;

    /// <summary>How much of the conversation stays visible to the acting user — <c>"Snapshot"</c> or <c>"Live"</c>.</summary>
    public string ScopeMode { get; set; } = string.Empty;

    /// <summary>When the share was created.</summary>
    public DateTime DateShared { get; set; }
}
