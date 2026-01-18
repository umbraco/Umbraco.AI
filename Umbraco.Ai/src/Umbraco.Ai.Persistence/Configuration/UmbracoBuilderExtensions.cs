using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Analytics;
using Umbraco.Ai.Core.Analytics.Usage;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Core.AuditLog;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Persistence;
using Umbraco.Ai.Persistence.Analytics;
using Umbraco.Ai.Persistence.Analytics.Usage;
using Umbraco.Ai.Persistence.Connections;
using Umbraco.Ai.Persistence.Context;
using Umbraco.Ai.Persistence.AuditLog;
using Umbraco.Ai.Persistence.Notifications;
using Umbraco.Ai.Persistence.Profiles;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for configuring Umbraco AI persistence.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds EF Core persistence for Umbraco AI.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiPersistence(this IUmbracoBuilder builder)
    {
        // Register DbContext using Umbraco's database provider detection with migrations assembly config
        builder.Services.AddUmbracoDbContext<UmbracoAiDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            ConfigureDatabaseProvider(options, connectionString, providerName);
        });

        // Replace in-memory repository with EF Core implementations (Singleton - IEFCoreScopeProvider manages scopes internally)
        builder.Services.AddSingleton<IAiConnectionRepository, EfCoreAiConnectionRepository>();
        builder.Services.AddSingleton<IAiProfileRepository, EfCoreAiProfileRepository>();
        builder.Services.AddSingleton<IAiContextRepository, EfCoreAiContextRepository>();
        builder.Services.AddSingleton<IAiAuditLogRepository, EfCoreAiAuditLogRepository>();
        builder.Services.AddSingleton<IAiUsageRecordRepository, EfCoreAiUsageRecordRepository>();
        builder.Services.AddSingleton<IAiUsageStatisticsRepository, EfCoreAiUsageStatisticsRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAiMigrationNotificationHandler>();

        return builder;
    }

    private static void ConfigureDatabaseProvider(
        DbContextOptionsBuilder options,
        string? connectionString,
        string? providerName)
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(providerName))
        {
            return;
        }

        // Configure provider with migrations assembly based on provider type
        switch (providerName)
        {
            case Constants.ProviderNames.SQLServer:
                options.UseSqlServer(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.Ai.Persistence.SqlServer"));
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.Ai.Persistence.Sqlite"));
                break;

            default:
                throw new InvalidOperationException(
                    $"The database provider '{providerName}' is not supported by Umbraco.Ai.Persistence. " +
                    $"Supported providers: SQL Server, SQLite.");
        }
    }
}
