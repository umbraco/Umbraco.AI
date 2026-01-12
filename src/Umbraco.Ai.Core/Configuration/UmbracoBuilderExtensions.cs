using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Core.Contexts.Middleware;
using Umbraco.Ai.Core.Contexts.Resolvers;
using Umbraco.Ai.Core.Contexts.ResourceTypes;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.EntityAdapter;
using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Core.Governance.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.RequestContext;
using Umbraco.Ai.Core.RequestContext.Processors;
using Umbraco.Ai.Core.Tools;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI services registration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    internal static IUmbracoBuilder AddUmbracoAiCore(this IUmbracoBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Config;

        // Prevent multiple registrations
        if (services.Any(x => x.ServiceType == typeof(AiProviderCollection)))
            return builder;

        // Bind AiOptions from "Umbraco:Ai" section
        services.Configure<AiOptions>(config.GetSection("Umbraco:Ai"));

        // Bind AiGovernanceOptions from "Umbraco:Ai:Governance" section
        services.Configure<AiGovernanceOptions>(config.GetSection("Umbraco:Ai:Governance"));

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
        services.AddSingleton<IAiEditableModelSchemaBuilder, AiEditableModelSchemaBuilder>();
        services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();

        // Auto-discover providers using TypeLoader (uses Umbraco's cached, efficient type discovery)
        builder.AiProviders()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiProvider, AiProviderAttribute>(cache: true));

        // Initialize middleware collection builders with default middleware
        // Use AiChatMiddleware() and AiEmbeddingMiddleware() extension methods to add/remove middleware in Composers
        builder.AiChatMiddleware()
            .Append<AiTelemetryChatMiddleware>()      // Telemetry first for accurate tracking
            .Append<AiContextInjectionChatMiddleware>();

        builder.AiEmbeddingMiddleware()
            .Append<AiTelemetryEmbeddingMiddleware>();  // Telemetry first for accurate tracking

        // Tool infrastructure - auto-discover tools via [AiTool] attribute
        builder.AiTools()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiTool, AiToolAttribute>(cache: true));

        // Function factory for creating MEAI AIFunction instances
        services.AddSingleton<IAiFunctionFactory, AiFunctionFactory>();

        // Editable model resolution
        services.AddSingleton<IAiEditableModelResolver, AiEditableModelResolver>();

        // Connection system
        services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
        services.AddSingleton<IAiConnectionService, AiConnectionService>();

        // Profile resolution
        services.AddSingleton<IAiProfileRepository, InMemoryAiProfileRepository>();
        services.AddSingleton<IAiProfileService, AiProfileService>();

        // Client factories
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        services.AddSingleton<IAiEmbeddingGeneratorFactory, AiEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAiChatService, AiChatService>();
        services.AddSingleton<IAiEmbeddingService, AiEmbeddingService>();
        // TODO: services.AddSingleton<IAiToolService, AiToolService>();

        // Context resource type infrastructure - auto-discover via [AiContextResourceType] attribute
        services.AddSingleton<IAiContextResourceTypeInfrastructure, AiContextResourceTypeInfrastructure>();
        builder.AiContextResourceTypes()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiContextResourceType, AiContextResourceTypeAttribute>(cache: true));

        // Context system
        services.AddSingleton<IAiContextRepository, InMemoryAiContextRepository>();
        services.AddSingleton<IAiContextService, AiContextService>();
        services.AddSingleton<IAiContextFormatter, AiContextFormatter>();
        services.AddSingleton<IAiContextAccessor, AiContextAccessor>();

        // Context resolution - pluggable resolver system
        // Order: Profile -> Content (content can override profile-level context)
        builder.AiContextResolvers()
            .Append<ProfileContextResolver>()
            .Append<ContentContextResolver>();
        services.AddSingleton<IAiContextResolutionService, AiContextResolutionService>();

        // Entity adapter infrastructure
        services.AddSingleton<IAiEntityContextHelper, AiEntityContextHelper>();

        // Request context processing - processes context items from frontend
        builder.AiRequestContextProcessors()
            .Append<SerializedEntityProcessor>()
            .Append<DefaultSystemMessageProcessor>();

        // Governance and tracing infrastructure
        // Note: IAiTraceRepository and IAiExecutionSpanRepository are registered by persistence layer
        services.AddSingleton<IAiTraceService, AiTraceService>();

        // Activity listener for OpenTelemetry integration
        services.AddSingleton<AiGovernanceActivityListener>();
        services.AddHostedService<AiGovernanceActivityListenerHost>();

        // Background job for trace cleanup
        services.AddHostedService<AiTraceCleanupBackgroundJob>();

        return builder;
    }
}
