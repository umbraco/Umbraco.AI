using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence.Notifications;
using Umbraco.AI.Agent.Persistence.Agents;
using Umbraco.AI.Core.Configuration;
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
        // Resolve AI connection string upfront (falls back to Umbraco CMS connection)
        var (aiConnectionString, aiProviderName) = AIConnectionStringResolver.Resolve(builder.Config);

        builder.Services.AddUmbracoDbContext<UmbracoAIAgentDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAIAgentDbContext.ConfigureProvider(options, aiConnectionString ?? connectionString, aiProviderName ?? providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAIAgentRepository, EFCoreAIAgentRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAgentMigrationNotificationHandler>();

        return builder;
    }

}
