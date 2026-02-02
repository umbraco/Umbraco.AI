using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Analytics.Usage.Middleware;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Middleware;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.AuditLog.Middleware;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.RuntimeContext.Contributors;
using Umbraco.AI.Core.RuntimeContext.Middleware;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.TaskQueue;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Web;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Prompt.Core.Media;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

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
        if (services.Any(x => x.ServiceType == typeof(AIProviderCollection)))
            return builder;

        // Bind AIOptions from "Umbraco:Ai" section
        services.Configure<AIOptions>(config.GetSection("Umbraco:Ai"));

        // Bind AIAuditLogOptions from "Umbraco:Ai:AuditLog" section
        services.Configure<AIAuditLogOptions>(config.GetSection("Umbraco:Ai:AuditLog"));

        // Bind AIAnalyticsOptions from "Umbraco:Ai:Analytics" section
        services.Configure<AIAnalyticsOptions>(config.GetSection("Umbraco:Ai:Analytics"));

        // Security infrastructure
        services.AddSingleton<IAiSensitiveFieldProtector, AISensitiveFieldProtector>();

        // Provider infrastructure
        services.AddSingleton<IAiCapabilityFactory, AICapabilityFactory>();
        services.AddSingleton<IAiEditableModelSchemaBuilder, AIEditableModelSchemaBuilder>();
        services.AddSingleton<IAiEditableModelSerializer, AIEditableModelSerializer>();
        services.AddSingleton<IAiProviderInfrastructure, AIProviderInfrastructure>();

        // Auto-discover providers using TypeLoader (uses Umbraco's cached, efficient type discovery)
        builder.AIProviders()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiProvider, AIProviderAttribute>(cache: true));

        // Initialize middleware collection builders with default middleware
        // Use AIChatMiddleware() and AIEmbeddingMiddleware() extension methods to add/remove middleware in Composers
        // Middleware is applied in order: first = innermost (closest to provider), last = outermost
        builder.AIChatMiddleware()
            .Append<AIRuntimeContextInjectingChatMiddleware>()  // Multimodal injection (innermost - before function invoking)
            .Append<AIFunctionInvokingChatMiddleware>()  // Function/tool invocation
            .Append<AITrackingChatMiddleware>()          // Tracks usage details (tokens, duration)
            .Append<AIUsageRecordingChatMiddleware>()    // Records usage to database for analytics
            .Append<AIAuditingChatMiddleware>()          // Audit logging (optional, can be disabled)
            .Append<AIContextInjectingChatMiddleware>(); // Context injection (outermost)

        builder.AIEmbeddingMiddleware()
            .Append<AITrackingEmbeddingMiddleware>()        // Tracks usage details
            .Append<AIUsageRecordingEmbeddingMiddleware>()  // Records usage to database for analytics
            .Append<AIAuditingEmbeddingMiddleware>();       // Audit logging (optional, can be disabled)

        // Tool infrastructure - auto-discover tools via [AITool] attribute
        builder.AITools()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiTool, AIToolAttribute>(cache: true));

        // Web fetch tool services
        services.AddSingleton<IUrlValidator, UrlValidator>();
        services.AddSingleton<IHtmlContentExtractor, HtmlContentExtractor>();
        services.AddSingleton<IWebContentFetcher, WebContentFetcher>();

        // HTTP client for web fetching
        services.AddHttpClient("WebFetchTool", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AIWebFetchOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "Umbraco.Ai.WebFetchTool/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler((sp) =>
        {
            var options = sp.GetRequiredService<IOptions<AIWebFetchOptions>>().Value;
            return new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = options.MaxRedirects,
                ServerCertificateCustomValidationCallback = null,
                UseProxy = true,
            };
        });

        // Configure web fetch options
        services.Configure<AIWebFetchOptions>(config.GetSection("Umbraco:Ai:Tools:WebFetch"));

        // Background task queue for async audit recording
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskQueueWorker>();

        // Function factory for creating MEAI AIFunction instances
        services.AddSingleton<IAiFunctionFactory, AIFunctionFactory>();

        // Editable model resolution
        services.AddSingleton<IAiEditableModelResolver, AIEditableModelResolver>();

        // Connection system
        services.AddSingleton<IAiConnectionRepository, InMemoryAiConnectionRepository>();
        services.AddSingleton<IAiConnectionService, AIConnectionService>();

        // Profile resolution
        services.AddSingleton<IAiProfileRepository, InMemoryAiProfileRepository>();
        services.AddSingleton<IAiProfileService, AIProfileService>();

        // Settings
        services.AddSingleton<IAiSettingsRepository, InMemoryAiSettingsRepository>();
        services.AddSingleton<IAiSettingsService, AISettingsService>();

        // Unified versioning service
        services.Configure<AIVersionCleanupPolicy>(config.GetSection("Umbraco:Ai:VersionCleanupPolicy"));
        services.AddSingleton<IAiEntityVersionService, AIEntityVersionService>();
        services.AddHostedService<AIVersionCleanupBackgroundJob>();

        // Versionable entity adapters for core entities
        builder.AIVersionableEntityAdapters()
            .Add<AIConnectionVersionableEntityAdapter>()
            .Add<AIProfileVersionableEntityAdapter>()
            .Add<AIContextVersionableEntityAdapter>();

        // Client factories
        services.AddSingleton<IAiChatClientFactory, AIChatClientFactory>();
        services.AddSingleton<IAiEmbeddingGeneratorFactory, AIEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAiChatService, AIChatService>();
        services.AddSingleton<IAiEmbeddingService, AIEmbeddingService>();
        // TODO: services.AddSingleton<IAiToolService, AIToolService>();

        // Context resource type infrastructure - auto-discover via [AIContextResourceType] attribute
        services.AddSingleton<IAiContextResourceTypeInfrastructure, AIContextResourceTypeInfrastructure>();
        builder.AIContextResourceTypes()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiContextResourceType, AIContextResourceTypeAttribute>(cache: true));

        // Context system
        services.AddSingleton<IAiContextRepository, InMemoryAiContextRepository>();
        services.AddSingleton<IAiContextService, AIContextService>();
        services.AddSingleton<IAiContextFormatter, AIContextFormatter>();
        services.AddSingleton<IAiContextAccessor, AIContextAccessor>();

        // Context resolution - pluggable resolver system
        // Order: Profile -> Content (content can override profile-level context)
        builder.AIContextResolvers()
            .Append<ProfileContextResolver>()
            .Append<ContentContextResolver>();
        services.AddSingleton<IAiContextResolutionService, AIContextResolutionService>();

        // Entity adapter infrastructure
        services.AddSingleton<IAiEntityContextHelper, AIEntityContextHelper>();

        // Runtime context infrastructure
        // Single instance implements both accessor (for reading) and scope provider (for creating)
        services.AddSingleton<AIRuntimeContextScopeProvider>();
        services.AddSingleton<IAiRuntimeContextAccessor>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());
        services.AddSingleton<IAiRuntimeContextScopeProvider>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());

        // Runtime context contributors - executed in order
        builder.AIRuntimeContextContributors()
            .Append<UserContextContributor>()           // Ambient: adds current user info
            .Append<SerializedEntityContributor>()      // Item-based: processes serialized entities
            .Append<DefaultSystemMessageContributor>(); // Fallback: handles remaining items

        // Register media image resolver
        builder.Services.AddSingleton<IAiUmbracoMediaResolver, AIUmbracoMediaResolver>();
        
        // AuditLog infrastructure
        // Note: IAiAuditLogRepository is registered by persistence layer
        services.AddSingleton<IAiAuditLogFactory, AIAuditLogFactory>();
        services.AddSingleton<IAiAuditLogService, AIAuditLogService>();

        // Background job for audit-log cleanup
        services.AddHostedService<AIAuditLogCleanupBackgroundJob>();

        // Analytics infrastructure
        // Note: IAiUsageRecordRepository and IAiUsageStatisticsRepository are registered by persistence layer
        services.AddSingleton<IAiUsageRecordFactory, AIUsageRecordFactory>();
        services.AddSingleton<IAiUsageRecordingService, AIUsageRecordingService>();
        services.AddSingleton<IAiUsageAggregationService, AIUsageAggregationService>();
        services.AddSingleton<IAiUsageAnalyticsService, AIUsageAnalyticsService>();

        // Background jobs for analytics aggregation and cleanup
        services.AddHostedService<AIUsageHourlyAggregationJob>();
        services.AddHostedService<AIUsageDailyRollupJob>();
        services.AddHostedService<AIUsageStatisticsCleanupJob>();

        return builder;
    }
}
