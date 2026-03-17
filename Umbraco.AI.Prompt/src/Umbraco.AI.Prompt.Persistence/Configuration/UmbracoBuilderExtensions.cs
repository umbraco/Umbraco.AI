using Microsoft.Extensions.DependencyInjection;
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
        // Register DbContext with provider-specific migrations assembly
        builder.Services.AddUmbracoDbContext<UmbracoAIPromptDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAIPromptDbContext.ConfigureProvider(options, connectionString, providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAIPromptRepository, EfCoreAIPromptRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunPromptMigrationNotificationHandler>();

        return builder;
    }

}
