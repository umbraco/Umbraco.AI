using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Conversations.Persistence.Configuration;

/// <summary>
/// Extension methods for registering Umbraco.AI.Agent.Conversations persistence.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds the Conversations EF Core persistence (DbContext, repositories, and the
    /// migration notification handler).
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIConversationsPersistence(this IUmbracoBuilder builder)
    {
        // TODO (Phase 2): register UmbracoAIConversationsDbContext, repositories,
        // and RunConversationsMigrationNotificationHandler.
        return builder;
    }
}
