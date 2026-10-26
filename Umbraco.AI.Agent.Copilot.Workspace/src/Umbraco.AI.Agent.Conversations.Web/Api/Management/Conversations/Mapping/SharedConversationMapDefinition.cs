using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;

/// <summary>
/// UmbracoMapper definitions for the recipient-side shared-conversation models.
/// <see cref="SharedConversationSummaryResponseModel.OwnerDisplayName"/> and
/// <see cref="SharedConversationResponseModel.OwnerDisplayName"/> are deliberately left unmapped
/// here — the domain models only carry the owner's <c>Guid</c> key, not a display name, and
/// resolving one needs <c>IUserService</c>, which has no natural home in a static mapping method
/// (same posture as <see cref="ConversationShareMapDefinition"/>'s <c>UserName</c>). Callers
/// (<c>SharedConversationResponseModelFactory</c>) resolve and set it after mapping.
/// <see cref="SharedConversationResponseModel.Messages"/> is also left unmapped — messages are
/// fetched separately from the conversation, same as <see cref="ConversationMapDefinition"/>'s own
/// <see cref="AIConversation"/> mapping never carries them either.
/// </summary>
internal sealed class SharedConversationMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AISharedConversationSummary, SharedConversationSummaryResponseModel>(
            (_, _) => new SharedConversationSummaryResponseModel(), MapToResponse);
        mapper.Define<AIConversation, SharedConversationResponseModel>(
            (_, _) => new SharedConversationResponseModel(), MapToResponse);
    }

    // Umbraco.Code.MapAll -OwnerDisplayName
    private static void MapToResponse(AISharedConversationSummary source, SharedConversationSummaryResponseModel target, MapperContext context)
    {
        target.ConversationId = source.ConversationId;
        target.Title = source.Title;
        target.ScopeMode = source.ScopeMode.ToString();
        target.DateShared = source.DateShared;
    }

    // Umbraco.Code.MapAll -OwnerDisplayName -Messages
    private static void MapToResponse(AIConversation source, SharedConversationResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Title = source.Title;
        target.AgentIdOrAlias = source.AgentIdOrAlias;
        target.ProfileId = source.ProfileId;
        target.ContextIds = source.ContextIds.ToArray();
        target.Resources = context.MapEnumerable<AIAttachedResource, ContextResourceModel>(source.Resources);
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
        target.LastMessageAt = source.LastMessageAt;
    }
}
