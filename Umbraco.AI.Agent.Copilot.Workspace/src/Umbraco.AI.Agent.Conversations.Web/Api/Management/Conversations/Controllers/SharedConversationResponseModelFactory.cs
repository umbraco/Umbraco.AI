using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;

/// <summary>
/// Maps the recipient-side shared-conversation domain types to their response models, resolving the
/// conversation owner's display name via <see cref="IUserService"/> along the way. Shared between
/// <see cref="SharedWithMeConversationsController"/> and <see cref="SharedViewConversationController"/> —
/// both need the same owner-name resolution, which can't live in a static <c>IMapDefinition</c>
/// method (no natural way to inject <see cref="IUserService"/> there) — the same reasoning as
/// <see cref="ConversationShareResponseModelFactory"/>.
/// </summary>
internal static class SharedConversationResponseModelFactory
{
    private const string UnknownUserDisplayName = "Unknown user";

    /// <summary>Maps a page of "shared with me" summaries, resolving each owner's display name.</summary>
    public static async Task<IReadOnlyList<SharedConversationSummaryResponseModel>> MapSummariesAsync(
        IUmbracoMapper umbracoMapper,
        IUserService userService,
        IReadOnlyList<AISharedConversationSummary> summaries)
    {
        var models = new List<SharedConversationSummaryResponseModel>(summaries.Count);
        foreach (var summary in summaries)
        {
            var model = umbracoMapper.Map<SharedConversationSummaryResponseModel>(summary)!;
            model.OwnerDisplayName = await ResolveOwnerDisplayNameAsync(userService, summary.OwnerUserKey);
            models.Add(model);
        }

        return models;
    }

    /// <summary>
    /// Maps a shared conversation and its visible messages, resolving the owner's display name. For a
    /// <see cref="AIConversationShareScopeMode.Snapshot"/> share, <c>dateModified</c>/<c>lastMessageAt</c>
    /// are frozen at the latest <see cref="AIMessage.DateCreated"/> among the already-bounded
    /// <paramref name="messages"/> (ARCHITECTURE.md decision 9) rather than the conversation's live
    /// fields — otherwise those two fields would quietly disclose that (and roughly when) the owner
    /// kept chatting after the cutoff, even though the recipient can never see what was said. Deriving
    /// this from <c>Max(DateCreated)</c> over the bounded list — not its last array element — matters
    /// specifically for the post-regenerate fallback in <c>IAIConversationShareReadService</c>: once
    /// the cutoff message itself is gone, there is no single "cutoff message" left to point at, only
    /// the bounded list itself, so the frozen date has to be derived from that list's own content, not
    /// assumed from its position. A <see cref="AIConversationShareScopeMode.Live"/> share has no
    /// ceiling, so its dates are left exactly as the conversation's own live values.
    /// </summary>
    public static async Task<SharedConversationResponseModel> MapAsync(
        IUmbracoMapper umbracoMapper,
        IUserService userService,
        AIConversation conversation,
        IReadOnlyList<AIMessage> messages,
        AIConversationShareScopeMode scopeMode)
    {
        var model = umbracoMapper.Map<SharedConversationResponseModel>(conversation)!;
        model.Messages = umbracoMapper.MapEnumerable<AIMessage, MessageResponseModel>(messages);
        model.OwnerDisplayName = await ResolveOwnerDisplayNameAsync(userService, conversation.UserKey);

        if (scopeMode == AIConversationShareScopeMode.Snapshot && messages.Count > 0)
        {
            // No messages to anchor to (decision 8 should normally prevent a brand-new Snapshot share
            // from ever reaching here with zero messages, but don't assume that holds for every code
            // path) falls back to the conversation's own fields — there's nothing hidden to leak either
            // way.
            var frozenDate = messages.Max(m => m.DateCreated);
            model.DateModified = frozenDate;
            model.LastMessageAt = frozenDate;
        }

        return model;
    }

    private static async Task<string> ResolveOwnerDisplayNameAsync(IUserService userService, Guid ownerUserKey)
    {
        var owner = await userService.GetAsync(ownerUserKey);
        return string.IsNullOrWhiteSpace(owner?.Name) ? UnknownUserDisplayName : owner.Name;
    }
}
