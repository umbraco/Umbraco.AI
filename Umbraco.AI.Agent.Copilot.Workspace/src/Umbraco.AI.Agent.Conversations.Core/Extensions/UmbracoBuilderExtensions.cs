using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Conversations.Extensions;

/// <summary>
/// Extension methods for registering Umbraco.AI.Agent.Conversations core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Conversations core services (domain services, repositories, and the
    /// <c>ConversationChatHistoryProvider</c> persistence bridge).
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIConversationsCore(this IUmbracoBuilder builder)
    {
        // TODO (Phase 3): register conversation/project services + ConversationChatHistoryProvider.
        return builder;
    }
}
