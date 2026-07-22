using Umbraco.AI.Web.Api.Management.Context.Models;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API response model for a conversation's metadata (messages are fetched separately).
/// </summary>
public sealed class ConversationResponseModel
{
    /// <summary>Unique identifier (also the AG-UI threadId).</summary>
    public Guid Id { get; set; }

    /// <summary>Optional owning project id.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>The agent id or alias this conversation runs (null when using "Auto").</summary>
    public string? AgentIdOrAlias { get; set; }

    /// <summary>Optional profile id override.</summary>
    public Guid? ProfileId { get; set; }

    /// <summary>Referenced <c>AIContext</c> ids attached to this conversation only.</summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>Resources attached to this conversation only.</summary>
    public IEnumerable<ContextResourceModel> Resources { get; set; } = [];

    /// <summary>Whether the conversation is pinned.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Whether the conversation is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime DateModified { get; set; }

    /// <summary>Timestamp of the most recent message.</summary>
    public DateTime? LastMessageAt { get; set; }
}
