using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Analytics;
using Umbraco.Ai.Core.Analytics.Usage;
using Umbraco.Ai.Core.Analytics.Usage.Middleware;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Core.Contexts.Middleware;
using Umbraco.Ai.Core.Contexts.Resolvers;
using Umbraco.Ai.Core.Contexts.ResourceTypes;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.EntityAdapter;
using Umbraco.Ai.Core.AuditLog;
using Umbraco.Ai.Core.AuditLog.Middleware;
using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.RuntimeContext.Contributors;
using Umbraco.Ai.Core.RuntimeContext.Middleware;
using Umbraco.Ai.Core.TaskQueue;
using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Core.Tools.Web;
using Umbraco.Ai.Prompt.Core.Media;
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

        // Bind AiAuditLogOptions from "Umbraco:Ai:AuditLog" section
        services.Configure<AiAuditLogOptions>(config.GetSection("Umbraco:Ai:AuditLog"));

        // Bind AiAnalyticsOptions from "Umbraco:Ai:Analytics" section
        services.Configure<AiAnalyticsOptions>(config.GetSection("Umbraco:Ai:Analytics"));

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AiCapabilityFactory>();
        services.AddSingleton<IAiEditableModelSchemaBuilder, AiEditableModelSchemaBuilder>();
        services.AddSingleton<IAiProviderInfrastructure, AiProviderInfrastructure>();

        // Auto-discover providers using TypeLoader (uses Umbraco's cached, efficient type discovery)
        builder.AiProviders()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiProvider, AiProviderAttribute>(cache: true));

        // Initialize middleware collection builders with default middleware
        // Use AiChatMiddleware() and AiEmbeddingMiddleware() extension methods to add/remove middleware in Composers
        // Middleware is applied in order: first = innermost (closest to provider), last = outermost
        builder.AiChatMiddleware()
            .Append<AiRuntimeContextInjectingChatMiddleware>()  // Multimodal injection (innermost - before function invoking)
            .Append<AiFunctionInvokingChatMiddleware>()  // Function/tool invocation
            .Append<AiTrackingChatMiddleware>()          // Tracks usage details (tokens, duration)
            .Append<AiUsageRecordingChatMiddleware>()    // Records usage to database for analytics
            .Append<AiAuditingChatMiddleware>()          // Audit logging (optional, can be disabled)
            .Append<AiContextInjectingChatMiddleware>(); // Context injection (outermost)

        builder.AiEmbeddingMiddleware()
            .Append<AiTrackingEmbeddingMiddleware>()        // Tracks usage details
            .Append<AiUsageRecordingEmbeddingMiddleware>()  // Records usage to database for analytics
            .Append<AiAuditingEmbeddingMiddleware>();       // Audit logging (optional, can be disabled)

        // Tool infrastructure - auto-discover tools via [AiTool] attribute
        builder.AiTools()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiTool, AiToolAttribute>(cache: true));

        // Web fetch tool services
        services.AddSingleton<IUrlValidator, UrlValidator>();
        services.AddSingleton<IHtmlContentExtractor, HtmlContentExtractor>();
        services.AddSingleton<IWebContentFetcher, WebContentFetcher>();

        // HTTP client for web fetching
        services.AddHttpClient("WebFetchTool", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AiWebFetchOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "Umbraco.Ai.WebFetchTool/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler((sp) =>
        {
            var options = sp.GetRequiredService<IOptions<AiWebFetchOptions>>().Value;
            return new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = options.MaxRedirects,
                ServerCertificateCustomValidationCallback = null,
                UseProxy = true,
            };
        });

        // Configure web fetch options
        services.Configure<AiWebFetchOptions>(config.GetSection("Umbraco:Ai:Tools:WebFetch"));

        // Background task queue for async audit recording
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskQueueWorker>();

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

        // Runtime context infrastructure
        // Single instance implements both accessor (for reading) and scope provider (for creating)
        services.AddSingleton<AiRuntimeContextScopeProvider>();
        services.AddSingleton<IAiRuntimeContextAccessor>(sp => sp.GetRequiredService<AiRuntimeContextScopeProvider>());
        services.AddSingleton<IAiRuntimeContextScopeProvider>(sp => sp.GetRequiredService<AiRuntimeContextScopeProvider>());

        // Runtime context contributors - processes context items from frontend
        builder.AiRuntimeContextContributors()
            .Append<SerializedEntityContributor>()
            .Append<DefaultSystemMessageContributor>();

        // Register media image resolver
        builder.Services.AddSingleton<IAiUmbracoMediaResolver, AiUmbracoMediaResolver>();
        
        // AuditLog infrastructure
        // Note: IAiAuditLogRepository is registered by persistence layer
        services.AddSingleton<IAiAuditLogFactory, AiAuditLogFactory>();
        services.AddSingleton<IAiAuditLogService, AiAuditLogService>();

        // Background job for audit-log cleanup
        services.AddHostedService<AiAuditLogCleanupBackgroundJob>();

        // Analytics infrastructure
        // Note: IAiUsageRecordRepository and IAiUsageStatisticsRepository are registered by persistence layer
        services.AddSingleton<IAiUsageRecordFactory, AiUsageRecordFactory>();
        services.AddSingleton<IAiUsageRecordingService, AiUsageRecordingService>();
        services.AddSingleton<IAiUsageAggregationService, AiUsageAggregationService>();
        services.AddSingleton<IAiUsageAnalyticsService, AiUsageAnalyticsService>();

        // Background jobs for analytics aggregation and cleanup
        services.AddHostedService<AiUsageHourlyAggregationJob>();
        services.AddHostedService<AiUsageDailyRollupJob>();
        services.AddHostedService<AiUsageStatisticsCleanupJob>();

        return builder;
    }
}
