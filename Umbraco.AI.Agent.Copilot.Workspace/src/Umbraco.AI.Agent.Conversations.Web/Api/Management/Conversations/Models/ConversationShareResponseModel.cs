namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API response model for one active recipient of a shared conversation.
/// </summary>
public sealed class ConversationShareResponseModel
{
    /// <summary>The key (GUID) of the recipient backoffice user.</summary>
    public Guid UserKey { get; set; }

    /// <summary>The recipient's display name, resolved at request time.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>How much of the conversation stays visible to this recipient — <c>"Snapshot"</c> or <c>"Live"</c>.</summary>
    public string ScopeMode { get; set; } = string.Empty;

    /// <summary>When the share was created.</summary>
    public DateTime DateShared { get; set; }
}
