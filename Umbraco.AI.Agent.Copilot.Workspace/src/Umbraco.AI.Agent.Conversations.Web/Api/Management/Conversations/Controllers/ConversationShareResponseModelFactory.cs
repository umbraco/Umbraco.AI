using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Maps <see cref="AIConversationShare"/> domain models to <see cref="ConversationShareResponseModel"/>,
/// resolving each recipient's display name via <see cref="IUserService"/> along the way. Shared between
/// <see cref="ShareConversationController"/> and <see cref="GetConversationSharesController"/> — both
/// return the same shape, and the name resolution can't live in a static <c>IMapDefinition</c> method
/// (no natural way to inject <see cref="IUserService"/> there).
/// </summary>
internal static class ConversationShareResponseModelFactory
{
    private const string UnknownUserDisplayName = "Unknown user";

    /// <summary>Maps a list of active shares, resolving each recipient's display name.</summary>
    public static async Task<IReadOnlyList<ConversationShareResponseModel>> MapAsync(
        IUmbracoMapper umbracoMapper,
        IUserService userService,
        IReadOnlyList<AIConversationShare> shares)
    {
        var models = new List<ConversationShareResponseModel>(shares.Count);
        foreach (var share in shares)
        {
            var model = umbracoMapper.Map<ConversationShareResponseModel>(share)!;
            var recipient = await userService.GetAsync(share.SharedWithUserKey);
            model.UserName = string.IsNullOrWhiteSpace(recipient?.Name) ? UnknownUserDisplayName : recipient.Name;
            models.Add(model);
        }

        return models;
    }
}
