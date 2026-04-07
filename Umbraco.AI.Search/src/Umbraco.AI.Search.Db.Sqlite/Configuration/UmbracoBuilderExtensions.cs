using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.AI.Search.Db;
using Umbraco.AI.Search.Db.Notifications;
using Umbraco.AI.Search.Db.VectorStore;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Search.Extensions;

/// <summary>
/// Extension methods for configuring SQLite vector store for Umbraco AI Search.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds SQLite persistence for Umbraco AI Search vector store.
    /// Uses brute-force in-memory similarity search with <c>TensorPrimitives.CosineSimilarity</c>.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoAISearchSqlite(this IUmbracoBuilder builder)
    {
        var (aiConnectionString, aiProviderName) = AIConnectionStringResolver.Resolve(builder.Config);

        // TODO: Pass shareUmbracoConnection: false when a custom connection string is configured.
        // Requires Umbraco CMS fix: https://github.com/umbraco/Umbraco-CMS/pull/22133
        builder.Services.AddUmbracoDbContext<UmbracoAISearchDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAISearchDbContext.ConfigureProvider(options, aiConnectionString ?? connectionString, aiProviderName ?? providerName);
        });

        builder.Services.AddSingleton<IAIVectorStore, EFCoreAIVectorStore>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAISearchMigrationNotificationHandler>();

        return builder;
    }
}
