using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Agui;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agent.Core.Context;
using Umbraco.Ai.Agent.Core.Models;
using Umbraco.Ai.Agent.Core.Scopes;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Core.Chat.Middleware;
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

        // Register agent factory (scoped - depends on scoped IAiChatService)
        builder.Services.AddSingleton<IAiAgentFactory, AiAgentFactory>();

        // Register AG-UI services
        builder.Services.AddSingleton<IAguiMessageConverter, AguiMessageConverter>();
        builder.Services.AddSingleton<IAguiToolConverter, AguiToolConverter>();
        builder.Services.AddSingleton<IAguiContextConverter, AguiContextConverter>();
        builder.Services.AddTransient<IAguiStreamingService, AguiStreamingService>();

        // Register agent context resolver
        builder.AiContextResolvers().Append<AgentContextResolver>();

        // Register tool reordering middleware before function invocation
        // This ensures server-side tools execute before frontend tools trigger termination
        builder.AiChatMiddleware().InsertBefore<AiFunctionInvokingChatMiddleware, AiToolReorderingChatMiddleware>();

        // Register versionable entity adapter for agents
        builder.AiVersionableEntityAdapters().Add<AiAgentVersionableEntityAdapter>();

        // Auto-discover agent scopes via [AiAgentScope] attribute
        builder.AiAgentScopes()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiAgentScope, AiAgentScopeAttribute>(cache: true));

        return builder;
    }
}
