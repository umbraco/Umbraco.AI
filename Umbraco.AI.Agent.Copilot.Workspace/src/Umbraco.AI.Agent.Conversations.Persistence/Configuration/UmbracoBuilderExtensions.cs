using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Persistence.Conversations;
using Umbraco.AI.Core.Configuration;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.AI.Agent.Conversations.Persistence.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Conversations.Persistence.Configuration;

/// <summary>
/// Extension methods for registering Umbraco.AI.Agent.Conversations persistence.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Conversations EF Core persistence (DbContext + migration notification handler).
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIConversationsPersistence(this IUmbracoBuilder builder)
    {
        // Resolve AI connection string upfront (falls back to Umbraco CMS connection).
        var (aiConnectionString, aiProviderName) = AIConnectionStringResolver.Resolve(builder.Config);

        builder.Services.AddUmbracoDbContext<UmbracoAIConversationsDbContext>(
            (options, connectionString, providerName, serviceProvider) =>
            {
                UmbracoAIConversationsDbContext.ConfigureProvider(options, aiConnectionString ?? connectionString, aiProviderName ?? providerName);
            },
            shareUmbracoConnection: aiConnectionString is null);

        // Conversation/message repository (EF Core). Project repository follows in Phase 3 alongside
        // the resource-type serializer its Resources collection depends on.
        builder.Services.AddSingleton<IAIConversationRepository, EFCoreAIConversationRepository>();

        // Register migration notification handler.
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunConversationsMigrationNotificationHandler>();

        return builder;
    }
}
