using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Persistence.Notifications;
using Umbraco.AI.Prompt.Persistence.Prompts;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Prompt.Persistence.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.AI.Prompt.Persistence services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.AI.Prompt persistence services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIPromptPersistence(this IUmbracoBuilder builder)
    {
        // Resolve AI connection string upfront (falls back to Umbraco CMS connection)
        var (aiConnectionString, aiProviderName) = AIConnectionStringResolver.Resolve(builder.Config);

        // TODO: Pass shareUmbracoConnection: false when a custom connection string is configured.
        // Requires Umbraco CMS fix: https://github.com/umbraco/Umbraco-CMS/pull/22133
        builder.Services.AddUmbracoDbContext<UmbracoAIPromptDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAIPromptDbContext.ConfigureProvider(options, aiConnectionString ?? connectionString, aiProviderName ?? providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAIPromptRepository, EFCoreAIPromptRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunPromptMigrationNotificationHandler>();

        return builder;
    }

}
