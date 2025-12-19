using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Persistence.Notifications;
using Umbraco.Ai.Agent.Persistence.Agents;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Ai.Agent.Persistence.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.Ai.Agent.Persistence services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.Ai.Agent persistence services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiAgentPersistence(this IUmbracoBuilder builder)
    {
        // Register DbContext with provider-specific migrations assembly
        builder.Services.AddUmbracoDbContext<UmbracoAiAgentDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            ConfigureDatabaseProvider(options, connectionString, providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAiAgentRepository, EfCoreAiAgentRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunPromptMigrationNotificationHandler>();

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

        switch (providerName)
        {
            case Constants.ProviderNames.SQLServer:
                options.UseSqlServer(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.Ai.Agent.Persistence.SqlServer"));
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.Ai.Agent.Persistence.Sqlite"));
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.Ai.Agent.");
        }
    }
}
