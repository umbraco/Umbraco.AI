using Umbraco.AI.Web.Api.Management.Context.Models;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API response model for a conversation shared with the acting user, including its visible
/// messages (<c>GET /conversations/{id}/shared-view</c>). Deliberately a separate shape from
/// <see cref="ConversationResponseModel"/> rather than a reuse of it: sharing has no owner-only
/// concepts (pinning, archiving, project membership, whether other shares exist), and needs the
/// owner's display name, which the owner's own response model never does since a conversation
/// never has to name its own owner to the owner.
/// </summary>
public sealed class SharedConversationResponseModel
{
    /// <summary>The shared conversation's id (also the AG-UI threadId).</summary>
    public Guid Id { get; set; }

    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>The agent id or alias this conversation runs (null when using "Auto").</summary>
    public string? AgentIdOrAlias { get; set; }

    /// <summary>Optional profile id override.</summary>
    public Guid? ProfileId { get; set; }

    /// <summary>The conversation owner's display name, resolved at request time.</summary>
    public string OwnerDisplayName { get; set; } = string.Empty;

    /// <summary>Referenced <c>AIContext</c> ids attached to this conversation — unfiltered, the same list the owner sees.</summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>Resources attached to this conversation — unfiltered, the same list the owner sees.</summary>
    public IEnumerable<ContextResourceModel> Resources { get; set; } = [];

    /// <summary>
    /// The conversation's visible messages, in ascending order — every message for a <c>Live</c>
    /// share, or only those at or before the <c>Snapshot</c> cutoff.
    /// </summary>
    public IEnumerable<MessageResponseModel> Messages { get; set; } = [];

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime DateModified { get; set; }

    /// <summary>Timestamp of the most recent message.</summary>
    public DateTime? LastMessageAt { get; set; }
}
