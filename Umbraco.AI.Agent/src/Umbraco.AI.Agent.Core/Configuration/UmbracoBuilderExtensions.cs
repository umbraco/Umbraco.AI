using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.Context;
using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Agent.Core.RuntimeContext;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Core.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.AI.Agent.Core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.AI.Agent core services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIAgentCore(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAIAgentService)))
        {
            return builder;
        }

        // Bind configuration
        builder.Services.Configure<AIAgentOptions>(
            builder.Config.GetSection(AIAgentOptions.SectionName));

        // Register in-memory repository as fallback (replaced by persistence layer)
        builder.Services.AddSingleton<IAIAgentRepository, InMemoryAIAgentRepository>();

        // Register scope validator
        builder.Services.AddSingleton<AIAgentScopeValidator>();

        // Register service
        builder.Services.AddSingleton<IAIAgentService, AIAgentService>();

        // Register agent factory (scoped - depends on scoped IAIChatService)
        builder.Services.AddSingleton<IAIAgentFactory, AIAgentFactory>();

        // Register AG-UI services
        builder.Services.AddSingleton<IAGUIMessageConverter, AGUIMessageConverter>();
        builder.Services.AddSingleton<IAGUIToolConverter, AGUIToolConverter>();
        builder.Services.AddSingleton<IAGUIContextConverter, AGUIContextConverter>();
        builder.Services.AddTransient<IAGUIStreamingService, AGUIStreamingService>();

        // Register agent context resolver
        builder.AIContextResolvers().Append<AgentContextResolver>();

        // Register surface context contributor
        builder.AIRuntimeContextContributors().Append<SurfaceContextContributor>();

        // Register tool reordering middleware before function invocation
        // This ensures server-side tools execute before frontend tools trigger termination
        builder.AIChatMiddleware().InsertBefore<AIFunctionInvokingChatMiddleware, AIToolReorderingChatMiddleware>();

        // Register versionable entity adapter for agents
        builder.AIVersionableEntityAdapters().Add<AIAgentVersionableEntityAdapter>();

        // Auto-discover agent surfaces via [AIAgentSurface] attribute
        builder.AIAgentSurfaces()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIAgentSurface, AIAgentSurfaceAttribute>(cache: true));

        return builder;
    }
}
