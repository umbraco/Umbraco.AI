using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.AI.Search.Db;
using Umbraco.AI.Search.Db.Notifications;
using Umbraco.AI.Search.Db.SqlServer.VectorStore;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Search.Extensions;

/// <summary>
/// Extension methods for configuring SQL Server vector store for Umbraco AI Search.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds SQL Server persistence for Umbraco AI Search vector store.
    /// Uses native <c>VECTOR_DISTANCE()</c> for server-side similarity search on SQL Server 2025+.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoAISearchSqlServer(this IUmbracoBuilder builder)
    {
        builder.Services.AddUmbracoDbContext<UmbracoAISearchDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAISearchDbContext.ConfigureProvider(options, connectionString, providerName);
        });

        builder.Services.AddSingleton<IAIVectorStore, SqlServerAIVectorStore>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAISearchMigrationNotificationHandler>();

        return builder;
    }
}
