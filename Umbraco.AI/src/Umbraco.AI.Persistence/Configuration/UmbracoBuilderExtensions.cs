using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Persistence;
using Umbraco.AI.Persistence.Analytics;
using Umbraco.AI.Persistence.Analytics.Usage;
using Umbraco.AI.Persistence.Connections;
using Umbraco.AI.Persistence.Context;
using Umbraco.AI.Persistence.AuditLog;
using Umbraco.AI.Persistence.Notifications;
using Umbraco.AI.Persistence.Profiles;
using Umbraco.AI.Persistence.Settings;
using Umbraco.AI.Persistence.Versioning;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Extensions;

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
        builder.Services.AddUmbracoDbContext<UmbracoAIDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            ConfigureDatabaseProvider(options, connectionString, providerName);
        });

        // Connection factory for entity/domain mapping with encryption support
        builder.Services.AddSingleton<IAIConnectionFactory, AIConnectionFactory>();

        // Replace in-memory repository with EF Core implementations (Singleton - IEFCoreScopeProvider manages scopes internally)
        builder.Services.AddSingleton<IAIConnectionRepository, EfCoreAiConnectionRepository>();
        builder.Services.AddSingleton<IAIProfileRepository, EfCoreAiProfileRepository>();
        builder.Services.AddSingleton<IAIContextRepository, EfCoreAiContextRepository>();
        builder.Services.AddSingleton<IAIAuditLogRepository, EfCoreAiAuditLogRepository>();
        builder.Services.AddSingleton<IAIUsageRecordRepository, EfCoreAiUsageRecordRepository>();
        builder.Services.AddSingleton<IAIUsageStatisticsRepository, EfCoreAiUsageStatisticsRepository>();
        builder.Services.AddSingleton<IAISettingsRepository, EfCoreAiSettingsRepository>();

        // Unified versioning repository
        builder.Services.AddSingleton<IAIEntityVersionRepository, EfCoreAiEntityVersionRepository>();

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
