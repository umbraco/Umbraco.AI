namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API request model for updating a conversation's metadata (rename, pin, archive, re-home to a
/// project, change agent/profile). Ownership is enforced server-side.
/// </summary>
public sealed class UpdateConversationRequestModel
{
    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>Owning project id (null detaches from any project).</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>The agent id or alias this conversation runs (null uses "Auto").</summary>
    public string? AgentIdOrAlias { get; set; }

    /// <summary>Optional profile id override.</summary>
    public Guid? ProfileId { get; set; }

    /// <summary>Whether the conversation is pinned.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Whether the conversation is archived.</summary>
    public bool IsArchived { get; set; }
}
