using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Agent.Core.Contexts;
using Umbraco.Ai.Agent.Core.Models;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Agent.Core.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.Ai.Agent.Core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.Ai.Agent core services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiAgentCore(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAiAgentService)))
        {
            return builder;
        }

        // Bind configuration
        builder.Services.Configure<AiAgentOptions>(
            builder.Config.GetSection(AiAgentOptions.SectionName));

        // Register in-memory repository as fallback (replaced by persistence layer)
        builder.Services.AddSingleton<IAiAgentRepository, InMemoryAiAgentRepository>();

        // Register service
        builder.Services.AddSingleton<IAiAgentService, AiAgentService>();

        // Register agent context resolver
        builder.AiContextResolvers().Append<AgentContextResolver>();

        return builder;
    }
}
