using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Conversations.Extensions;

/// <summary>
/// Extension methods for registering Umbraco.AI.Agent.Conversations core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Conversations core services, including the <see cref="ConversationChatHistoryProvider"/>
    /// persistence bridge.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIConversationsCore(this IUmbracoBuilder builder)
    {
        // Prevent duplicate registration.
        if (builder.Services.Any(x => x.ServiceType == typeof(ConversationChatHistoryProvider)))
        {
            return builder;
        }

        // A single shared instance (holds no session-specific state) — the repository (registered by
        // the persistence layer) is resolved lazily. Registered via a factory because the provider's
        // constructor takes the internal IAIConversationRepository.
        builder.Services.AddSingleton(sp =>
            new ConversationChatHistoryProvider(sp.GetRequiredService<IAIConversationRepository>()));

        return builder;
    }
}
