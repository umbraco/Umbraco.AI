using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.AI.Search.EfCore;
using Umbraco.AI.Search.EfCore.Notifications;
using Umbraco.AI.Search.EfCore.VectorStore;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Search.Extensions;

/// <summary>
/// Extension methods for configuring Umbraco AI Search persistence.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds EF Core persistence for Umbraco AI Search vector store.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAISearchPersistence(this IUmbracoBuilder builder)
    {
        // Register DbContext using Umbraco's database provider detection with migrations assembly config
        builder.Services.AddUmbracoDbContext<UmbracoAISearchDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAISearchDbContext.ConfigureProvider(options, connectionString, providerName);
        });

        // Replace in-memory vector store with EF Core implementation
        builder.Services.AddSingleton<IAIVectorStore, EfCoreAIVectorStore>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAISearchMigrationNotificationHandler>();

        return builder;
    }
}
