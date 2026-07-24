using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;

/// <summary>
/// UmbracoMapper definitions for conversation models.
/// </summary>
internal sealed class ConversationMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AIConversation, ConversationResponseModel>((_, _) => new ConversationResponseModel(), MapToResponse);
        mapper.Define<AIMessage, MessageResponseModel>((_, _) => new MessageResponseModel(), MapToResponse);
        mapper.Define<CreateConversationRequestModel, AIConversation>((_, _) => new AIConversation(), MapFromCreate);
        mapper.Define<UpdateConversationRequestModel, AIConversation>((_, _) => new AIConversation(), MapFromUpdate);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AIConversation source, ConversationResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.ProjectId = source.ProjectId;
        target.Title = source.Title;
        target.AgentIdOrAlias = source.AgentIdOrAlias;
        target.ProfileId = source.ProfileId;
        target.ContextIds = source.ContextIds.ToArray();
        target.Resources = context.MapEnumerable<AIAttachedResource, ContextResourceModel>(source.Resources);
        target.IsPinned = source.IsPinned;
        target.IsArchived = source.IsArchived;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
        target.LastMessageAt = source.LastMessageAt;
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AIMessage source, MessageResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Sequence = source.Sequence;
        target.Role = source.Role;
        target.ContentJson = source.ContentJson;
        target.ContentText = source.ContentText;
        target.InputTokens = source.InputTokens;
        target.OutputTokens = source.OutputTokens;
        target.DateCreated = source.DateCreated;
    }

    // Umbraco.Code.MapAll -Id -UserKey -ContextIds -Resources -IsPinned -IsArchived -DateCreated -DateModified -LastMessageAt -Version
    private static void MapFromCreate(CreateConversationRequestModel source, AIConversation target, MapperContext context)
    {
        // A new conversation starts with no context/resource overrides of its own.
        target.ProjectId = source.ProjectId;
        target.Title = source.Title;
        target.AgentIdOrAlias = source.AgentIdOrAlias;
        target.ProfileId = source.ProfileId;
    }

    // Umbraco.Code.MapAll -Id -UserKey -DateCreated -DateModified -LastMessageAt -Version
    private static void MapFromUpdate(UpdateConversationRequestModel source, AIConversation target, MapperContext context)
    {
        target.Title = source.Title;
        target.ProjectId = source.ProjectId;
        target.AgentIdOrAlias = source.AgentIdOrAlias;
        target.ProfileId = source.ProfileId;
        target.ContextIds = source.ContextIds.ToList();
        target.Resources = context.MapEnumerable<ContextResourceModel, AIAttachedResource>(source.Resources);
        target.IsPinned = source.IsPinned;
        target.IsArchived = source.IsArchived;
    }
}
