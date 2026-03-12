using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence.Notifications;
using Umbraco.AI.Agent.Persistence.Agents;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Persistence.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.AI.Agent.Persistence services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.AI.Agent persistence services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIAgentPersistence(this IUmbracoBuilder builder)
    {
        // Register migration connection config singleton so the migration handler can create a
        // clean DbContext that bypasses MiniProfiler connection wrapping. In Development mode,
        // Umbraco wraps SQLite connections with ProfiledDbConnection, whose ConnectionString getter
        // returns null and causes NullReferenceException in SqliteDatabaseCreator.Exists().
        var migrationConfig = new MigrationConnectionConfig();
        builder.Services.AddSingleton(migrationConfig);

        // Register DbContext with provider-specific migrations assembly
        builder.Services.AddUmbracoDbContext<UmbracoAIAgentDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            migrationConfig.ConnectionString = connectionString;
            migrationConfig.ProviderName = providerName;
            ConfigureDatabaseProvider(options, connectionString, providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAIAgentRepository, EfCoreAIAgentRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAgentMigrationNotificationHandler>();

        return builder;
    }

    internal static void ConfigureDatabaseProvider(
        DbContextOptionsBuilder options,
        string? connectionString,
        string? providerName)
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(providerName))
        {
            return;
        }

        switch (providerName)
        {
            case Constants.ProviderNames.SQLServer:
                options.UseSqlServer(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.SqlServer"));
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.Sqlite"));
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.AI.Agent.");
        }
    }
}

/// <summary>
/// Holds connection details captured from Umbraco's database configuration for use when
/// creating a clean migration DbContext that bypasses MiniProfiler connection wrapping.
/// </summary>
internal sealed class MigrationConnectionConfig
{
    public string? ConnectionString { get; set; }
    public string? ProviderName { get; set; }
}
