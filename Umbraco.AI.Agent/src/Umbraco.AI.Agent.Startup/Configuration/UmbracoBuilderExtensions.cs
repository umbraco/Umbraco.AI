using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Configuration;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence.Configuration;
using Umbraco.AI.Agent.Web.Configuration;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for adding all Umbraco.Ai.Agent services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds all Umbraco.Ai.Agent services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiAgent(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAiAgentService)))
        {
            return builder;
        }

        builder.AddUmbracoAiAgentCore();
        builder.AddUmbracoAiAgentPersistence();
        builder.AddUmbracoAiAgentWeb();

        return builder;
    }
}
