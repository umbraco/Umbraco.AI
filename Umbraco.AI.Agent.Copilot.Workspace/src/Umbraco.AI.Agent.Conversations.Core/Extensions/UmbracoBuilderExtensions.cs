using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

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
        // the persistence layer) and file store (registered by Umbraco.AI.Agent) are resolved lazily.
        // Registered via a factory because the provider's constructor takes the internal
        // IAIConversationRepository.
        builder.Services.AddSingleton(sp =>
            new ConversationChatHistoryProvider(
                sp.GetRequiredService<IAIConversationRepository>(),
                sp.GetRequiredService<IAIFileStore>()));

        // Ownership-enforcing services over the internal repositories (repos registered by the
        // persistence layer). Controllers and the stream endpoint go through these, never the repos.
        builder.Services.AddScoped<IAIConversationService, AIConversationService>();
        builder.Services.AddScoped<IAIProjectService, AIProjectService>();

        // Block deleting a project that still owns conversations (mirrors the connection/profile guard).
        builder.AddNotificationAsyncHandler<AIProjectDeletingNotification, AIProjectDeletingNotificationHandler>();

        return builder;
    }
}
