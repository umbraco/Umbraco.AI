using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Persistence.Notifications;
using Umbraco.Ai.Persistence.Repositories;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Ai.Persistence.Extensions;

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
        builder.Services.AddUmbracoDbContext<UmbracoAiDbContext>((serviceProvider, options, connectionString, providerName) =>
        {
            ConfigureDatabaseProvider(options, connectionString, providerName);
        });

        // Replace in-memory repositories with EF Core implementations
        builder.Services.AddScoped<IAiConnectionRepository, EfCoreAiConnectionRepository>();
        builder.Services.AddScoped<IAiProfileRepository, EfCoreAiProfileRepository>();

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
