using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.AI.Search.SqlServer;
using Umbraco.AI.Search.SqlServer.Notifications;
using Umbraco.AI.Search.SqlServer.VectorStore;
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
    /// Uses native <c>vector</c> type and <c>VECTOR_DISTANCE()</c> for server-side similarity search.
    /// </summary>
    public static IUmbracoBuilder AddUmbracoAISearchSqlServer(this IUmbracoBuilder builder)
    {
        builder.Services.AddUmbracoDbContext<UmbracoAISearchDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            options.UseSqlServer(connectionString, x =>
                x.MigrationsAssembly(typeof(SqlServerAIVectorStore).Assembly.FullName));
        });

        builder.Services.AddSingleton<IAIVectorStore, SqlServerAIVectorStore>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAISearchSqlServerMigrationNotificationHandler>();

        return builder;
    }
}
