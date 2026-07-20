using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Conversations.Persistence.Conversations;
using Umbraco.AI.Agent.Conversations.Persistence.Projects;
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

        // Conversation/message repository (EF Core).
        builder.Services.AddSingleton<IAIConversationRepository, EFCoreAIConversationRepository>();

        // Project repository (EF Core) + its factory, which reuses the core resource-type collection
        // and editable-model serializer for schema-driven settings (de)serialization.
        builder.Services.AddSingleton<AIProjectFactory>();
        builder.Services.AddSingleton<IAIProjectRepository, EFCoreAIProjectRepository>();

        // Register migration notification handler.
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunConversationsMigrationNotificationHandler>();

        return builder;
    }
}
