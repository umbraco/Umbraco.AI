using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Agui;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.Context;
using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Agent.Core.Scopes;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Core.Configuration;

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
        builder.Services.Configure<AIAgentOptions>(
            builder.Config.GetSection(AIAgentOptions.SectionName));

        // Register in-memory repository as fallback (replaced by persistence layer)
        builder.Services.AddSingleton<IAiAgentRepository, InMemoryAiAgentRepository>();

        // Register service
        builder.Services.AddSingleton<IAiAgentService, AIAgentService>();

        // Register agent factory (scoped - depends on scoped IAiChatService)
        builder.Services.AddSingleton<IAiAgentFactory, AIAgentFactory>();

        // Register AG-UI services
        builder.Services.AddSingleton<IAguiMessageConverter, AguiMessageConverter>();
        builder.Services.AddSingleton<IAguiToolConverter, AguiToolConverter>();
        builder.Services.AddSingleton<IAguiContextConverter, AguiContextConverter>();
        builder.Services.AddTransient<IAguiStreamingService, AguiStreamingService>();

        // Register agent context resolver
        builder.AIContextResolvers().Append<AgentContextResolver>();

        // Register tool reordering middleware before function invocation
        // This ensures server-side tools execute before frontend tools trigger termination
        builder.AIChatMiddleware().InsertBefore<AIFunctionInvokingChatMiddleware, AIToolReorderingChatMiddleware>();

        // Register versionable entity adapter for agents
        builder.AIVersionableEntityAdapters().Add<AIAgentVersionableEntityAdapter>();

        // Auto-discover agent scopes via [AIAgentScope] attribute
        builder.AIAgentScopes()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiAgentScope, AIAgentScopeAttribute>(cache: true));

        return builder;
    }
}
