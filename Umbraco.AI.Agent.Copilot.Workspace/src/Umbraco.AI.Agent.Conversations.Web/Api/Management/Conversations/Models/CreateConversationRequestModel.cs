using Umbraco.AI.Web.Api.Management.Context.Models;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;

/// <summary>
/// API request model for creating a conversation. The owning user is taken from the acting backoffice
/// user, never the client. Contexts and resources are accepted up front so a client can persist an
/// unsaved draft's own attachments in the same request that creates it.
/// </summary>
public sealed class CreateConversationRequestModel
{
    /// <summary>Optional owning project id.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Optional initial title (auto-generated from the first exchange when omitted).</summary>
    public string? Title { get; set; }

    /// <summary>Optional agent id or alias to run (null uses "Auto").</summary>
    public string? AgentIdOrAlias { get; set; }

    /// <summary>Optional profile id override.</summary>
    public Guid? ProfileId { get; set; }

    /// <summary>Referenced <c>AIContext</c> ids attached to this conversation only.</summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>Resources attached to this conversation only.</summary>
    public IEnumerable<ContextResourceModel> Resources { get; set; } = [];
}
