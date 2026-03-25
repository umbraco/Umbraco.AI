using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Sqlite;
using Umbraco.AI.Search.Sqlite.Notifications;
using Umbraco.AI.Search.Sqlite.VectorStore;
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
    /// </summary>
    public static IUmbracoBuilder AddUmbracoAISearchSqlite(this IUmbracoBuilder builder)
    {
        builder.Services.AddUmbracoDbContext<UmbracoAISearchDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            if (providerName == Umbraco.Cms.Core.Constants.ProviderNames.SQLLite
                || providerName == "Microsoft.Data.SQLite")
            {
                options.UseSqlite(connectionString, x =>
                    x.MigrationsAssembly(typeof(SqliteAIVectorStore).Assembly.FullName));
            }
        });

        builder.Services.AddSingleton<SqliteAIVectorStore>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAISearchSqliteMigrationNotificationHandler>();

        return builder;
    }
}
