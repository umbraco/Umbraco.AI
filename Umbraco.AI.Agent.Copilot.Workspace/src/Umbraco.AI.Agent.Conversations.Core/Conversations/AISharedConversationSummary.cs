namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// One row of the recipient's "shared with me" list: an active <see cref="AIConversationShare"/>
/// joined with the owning conversation's own display fields, so the paged query needs no follow-up
/// read per row.
/// </summary>
internal sealed class AISharedConversationSummary
{
    /// <summary>The shared conversation's id.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>The shared conversation's display title.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// The key (GUID) of the backoffice user who owns the conversation. This assembly has no user
    /// table to resolve a display name from — resolving <c>OwnerUserKey</c> to a display name
    /// happens in the service or Web layer, which already needs to do the same resolution for the
    /// owner-side <c>GET .../shares</c> response's <c>userName</c> field.
    /// </summary>
    public Guid OwnerUserKey { get; set; }

    /// <summary>The scope mode this share was granted with.</summary>
    public AIConversationShareScopeMode ScopeMode { get; set; }

    /// <summary>When the share was created.</summary>
    public DateTime DateShared { get; set; }
}
