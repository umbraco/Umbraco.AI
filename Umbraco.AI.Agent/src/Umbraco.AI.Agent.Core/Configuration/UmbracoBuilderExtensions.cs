using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.Agent.Core.Context;
using Umbraco.AI.Agent.Core.Guardrails;
using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Agent.Core.RuntimeContext;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Agent.Core.Telemetry;
using Umbraco.AI.Agent.Core.Workflows;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

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

        // Register in-memory repositories as fallback (replaced by persistence layer)
        builder.Services.AddSingleton<IAIAgentRepository, InMemoryAIAgentRepository>();

        // Register scope validator
        builder.Services.AddSingleton<AIAgentScopeValidator>();

        // Register services
        builder.Services.AddSingleton<IAIAgentService, AIAgentService>();
        // Prevent deletion of profiles referenced by agents
        builder.AddNotificationAsyncHandler<AIProfileDeletingNotification, AIProfileDeletingAgentNotificationHandler>();

        // Register agent factory (scoped - depends on scoped IAIChatService)
        builder.Services.AddSingleton<IAIAgentFactory, AIAgentFactory>();

        // Register AG-UI services
        builder.Services.AddSingleton<IAGUIMessageConverter, AGUIMessageConverter>();
        builder.Services.AddSingleton<IAGUIToolConverter, AGUIToolConverter>();
        builder.Services.AddSingleton<IAGUIContextConverter, AGUIContextConverter>();
        // Conversation uploads are private user content, so the store is rooted under the content root
        // rather than on the media file system. The media file system lives inside the web root and is
        // served at /media, which made every upload anonymously downloadable no matter what the
        // management API allowed. Registered by factory so the path stays an implementation detail;
        // replace IAIFileStore itself to move storage elsewhere (a private blob container, say).
        builder.Services.AddSingleton<IAILegacyPublicFileCleanup, AILegacyPublicFileCleanup>();
        builder.Services.AddSingleton<IAIFileStore>(factory =>
        {
            var ioHelper = factory.GetRequiredService<IIOHelper>();
            var hostingEnvironment = factory.GetRequiredService<IHostingEnvironment>();

            // The path is content-root-relative and tilde-prefixed, so it needs Umbraco's mapper. The
            // suggested IHostEnvironment replacement does not exist in the CMS version we target.
#pragma warning disable CS0618 // MapPathContentRoot - no replacement available on this CMS version
            var rootPath = hostingEnvironment.MapPathContentRoot(Constants.SystemDirectories.ConversationFiles);
#pragma warning restore CS0618

            // PhysicalFileSystem requires a non-empty root URL, but this file system is deliberately
            // not addressable and the store never asks it for a URL.
            var fileSystem = new PhysicalFileSystem(
                ioHelper,
                hostingEnvironment,
                factory.GetRequiredService<ILogger<PhysicalFileSystem>>(),
                rootPath,
                rootUrl: "/__umbraco-ai-agent-conversation-files-not-served__");

            return new AIFileStore(
                fileSystem,
                factory.GetRequiredService<ILogger<AIFileStore>>(),
                factory.GetService<IBackOfficeSecurityAccessor>());
        });
        builder.Services.AddSingleton<IAGUIFileProcessor, AGUIFileProcessor>();
        builder.Services.AddTransient<IAGUIStreamingService, AGUIStreamingService>();
        builder.Services.AddHostedService<AIFileCleanupBackgroundJob>();

        // Register agent context resolver
        builder.AIContextResolvers().Append<AgentContextResolver>();

        // Register agent guardrail resolver (runs after profile resolver)
        builder.AIGuardrailResolvers().Append<AgentGuardrailResolver>();

        // Register surface context contributor, then the contextual-editing guidance contributor
        // (which reads the surface the former resolves — order matters).
        builder.AIRuntimeContextContributors().Append<SurfaceContextContributor>();
        builder.AIRuntimeContextContributors().Append<ContextualEditingGuidanceContributor>();

        // Register tool reordering middleware before function invocation
        // This ensures server-side tools execute before frontend tools trigger termination
        builder.AIChatMiddleware().InsertBefore<AIFunctionInvokingChatMiddleware, AIToolReorderingChatMiddleware>();

        // Register the agent system message middleware outermost (appended last), so the runtime-context
        // block lands at index 0 of history + new turn, before the context injector looks for a system
        // message to extend and before the audit log snapshots the prompt.
        builder.AIChatMiddleware().Append<AIAgentSystemMessageChatMiddleware>();

        // Register versionable entity adapters for agents
        builder.AIVersionableEntityAdapters().Add<AIAgentVersionableEntityAdapter>();

        // Auto-discover agent surfaces via [AIAgentSurface] attribute
        builder.AIAgentSurfaces()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIAgentSurface, AIAgentSurfaceAttribute>(cache: true));

        // Auto-discover agent workflows via [AIAgentWorkflow] attribute
        builder.AIAgentWorkflows()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIAgentWorkflow, AIAgentWorkflowAttribute>(cache: true));

        // Usage telemetry - contributes anonymous aggregate counts to the CMS telemetry report
        builder.Services.AddTransient<IDetailedTelemetryProvider, AIAgentUsageTelemetryProvider>();

        return builder;
    }
}
