using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;

/// <summary>
/// UmbracoMapper definitions for conversation share models. <see cref="ConversationShareResponseModel.UserName"/>
/// is deliberately left unmapped here — the domain model only carries the recipient's <c>Guid</c> key,
/// not a display name, and resolving one needs <c>IUserService</c>, which has no natural home in a
/// static mapping method. Callers (the share controllers) resolve and set it after mapping.
/// </summary>
internal sealed class ConversationShareMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AIConversationShare, ConversationShareResponseModel>(
            (_, _) => new ConversationShareResponseModel(), MapToResponse);
    }

    // Umbraco.Code.MapAll -UserName
    private static void MapToResponse(AIConversationShare source, ConversationShareResponseModel target, MapperContext context)
    {
        target.UserKey = source.SharedWithUserKey;
        target.ScopeMode = source.ScopeMode.ToString();
        target.DateShared = source.DateShared;
    }
}
