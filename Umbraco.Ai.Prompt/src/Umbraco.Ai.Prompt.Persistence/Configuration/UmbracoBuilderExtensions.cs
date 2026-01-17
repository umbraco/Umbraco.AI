using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Persistence.Notifications;
using Umbraco.Ai.Prompt.Persistence.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Ai.Prompt.Persistence.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.Ai.Prompt.Persistence services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.Ai.Prompt persistence services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiPromptPersistence(this IUmbracoBuilder builder)
    {
        // Register DbContext with provider-specific migrations assembly
        builder.Services.AddUmbracoDbContext<UmbracoAiPromptDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            ConfigureDatabaseProvider(options, connectionString, providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAiPromptRepository, EfCoreAiPromptRepository>();

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
                    x.MigrationsAssembly("Umbraco.Ai.Prompt.Persistence.SqlServer"));
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.Ai.Prompt.Persistence.Sqlite"));
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.Ai.Prompt.");
        }
    }
}
