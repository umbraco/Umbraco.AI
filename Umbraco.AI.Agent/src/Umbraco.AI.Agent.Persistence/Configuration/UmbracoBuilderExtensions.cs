using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence.Notifications;
using Umbraco.AI.Agent.Persistence.Agents;
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
        // Register DbContext with provider-specific migrations assembly
        builder.Services.AddUmbracoDbContext<UmbracoAIAgentDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAIAgentDbContext.ConfigureProvider(options, connectionString, providerName);
        });

        // Replace in-memory repository with EF Core implementation
        builder.Services.AddSingleton<IAIAgentRepository, EfCoreAIAgentRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAgentMigrationNotificationHandler>();

        return builder;
    }

}
