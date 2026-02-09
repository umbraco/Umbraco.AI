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
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Core.Tests.Graders;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
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
    internal static IUmbracoBuilder AddUmbracoAICore(this IUmbracoBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Config;

        // Prevent multiple registrations
        if (services.Any(x => x.ServiceType == typeof(AIProviderCollection)))
            return builder;

        // Bind AIOptions from "Umbraco:AI" section
        services.Configure<AIOptions>(config.GetSection("Umbraco:AI"));

        // Bind AIAuditLogOptions from "Umbraco:AI:AuditLog" section
        services.Configure<AIAuditLogOptions>(config.GetSection("Umbraco:AI:AuditLog"));

        // Bind AIAnalyticsOptions from "Umbraco:AI:Analytics" section
        services.Configure<AIAnalyticsOptions>(config.GetSection("Umbraco:AI:Analytics"));

        // Security infrastructure
        services.AddSingleton<IAISensitiveFieldProtector, AISensitiveFieldProtector>();

        // Provider infrastructure
        services.AddSingleton<IAICapabilityFactory, AICapabilityFactory>();
        services.AddSingleton<IAIEditableModelSchemaBuilder, AIEditableModelSchemaBuilder>();
        services.AddSingleton<IAIEditableModelSerializer, AIEditableModelSerializer>();
        services.AddSingleton<IAIProviderInfrastructure, AIProviderInfrastructure>();

        // Auto-discover providers using TypeLoader (uses Umbraco's cached, efficient type discovery)
        builder.AIProviders()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIProvider, AIProviderAttribute>(cache: true));

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
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAITool, AIToolAttribute>(cache: true));

        // Tool scope infrastructure - auto-discover scopes via [AIToolScope] attribute
        builder.AIToolScopes()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIToolScope, AIToolScopeAttribute>(cache: true));

        // Web fetch tool services
        services.AddSingleton<IUrlValidator, UrlValidator>();
        services.AddSingleton<IHtmlContentExtractor, HtmlContentExtractor>();
        services.AddSingleton<IWebContentFetcher, WebContentFetcher>();

        // HTTP client for web fetching
        services.AddHttpClient("WebFetchTool", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AIWebFetchOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "Umbraco.AI.WebFetchTool/1.0");
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
        services.Configure<AIWebFetchOptions>(config.GetSection("Umbraco:AI:Tools:WebFetch"));

        // Background task queue for async audit recording
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskQueueWorker>();

        // Function factory for creating MEAI AIFunction instances
        services.AddSingleton<IAIFunctionFactory, AIFunctionFactory>();

        // Editable model resolution
        services.AddSingleton<IAIEditableModelResolver, AIEditableModelResolver>();

        // Connection system
        services.AddSingleton<IAIConnectionRepository, InMemoryAIConnectionRepository>();
        services.AddSingleton<IAIConnectionService, AIConnectionService>();

        // Profile resolution
        services.AddSingleton<IAIProfileRepository, InMemoryAIProfileRepository>();
        services.AddSingleton<IAIProfileService, AIProfileService>();

        // Settings
        services.AddSingleton<IAISettingsRepository, InMemoryAISettingsRepository>();
        services.AddSingleton<IAISettingsService, AISettingsService>();

        // Unified versioning service
        services.Configure<AIVersionCleanupPolicy>(config.GetSection("Umbraco:AI:VersionCleanupPolicy"));
        services.AddSingleton<IAIEntityVersionService, AIEntityVersionService>();
        services.AddHostedService<AIVersionCleanupBackgroundJob>();

        // Versionable entity adapters for core entities
        builder.AIVersionableEntityAdapters()
            .Add<AIConnectionVersionableEntityAdapter>()
            .Add<AIProfileVersionableEntityAdapter>()
            .Add<AIContextVersionableEntityAdapter>();

        // Client factories
        services.AddSingleton<IAIChatClientFactory, AIChatClientFactory>();
        services.AddSingleton<IAIEmbeddingGeneratorFactory, AIEmbeddingGeneratorFactory>();

        // High-level services
        services.AddSingleton<IAIChatService, AIChatService>();
        services.AddSingleton<IAIEmbeddingService, AIEmbeddingService>();
        // TODO: services.AddSingleton<IAIToolService, AIToolService>();

        // Context resource type infrastructure - auto-discover via [AIContextResourceType] attribute
        services.AddSingleton<IAIContextResourceTypeInfrastructure, AIContextResourceTypeInfrastructure>();
        builder.AIContextResourceTypes()
            .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAIContextResourceType, AIContextResourceTypeAttribute>(cache: true));

        // Context system
        services.AddSingleton<IAIContextRepository, InMemoryAIContextRepository>();
        services.AddSingleton<IAIContextService, AIContextService>();
        services.AddSingleton<IAIContextFormatter, AIContextFormatter>();
        services.AddSingleton<IAIContextAccessor, AIContextAccessor>();

        // Context resolution - pluggable resolver system
        // Order: Profile -> Content (content can override profile-level context)
        builder.AIContextResolvers()
            .Append<ProfileContextResolver>()
            .Append<ContentContextResolver>();
        services.AddSingleton<IAIContextResolutionService, AIContextResolutionService>();

        // Entity adapter infrastructure
        services.AddSingleton<IAIEntityContextHelper, AIEntityContextHelper>();

        // Runtime context infrastructure
        // Single instance implements both accessor (for reading) and scope provider (for creating)
        services.AddSingleton<AIRuntimeContextScopeProvider>();
        services.AddSingleton<IAIRuntimeContextAccessor>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());
        services.AddSingleton<IAIRuntimeContextScopeProvider>(sp => sp.GetRequiredService<AIRuntimeContextScopeProvider>());

        // Runtime context contributors - executed in order
        builder.AIRuntimeContextContributors()
            .Append<UserContextContributor>()           // Ambient: adds current user info
            .Append<SerializedEntityContributor>()      // Item-based: processes serialized entities
            .Append<DefaultSystemMessageContributor>(); // Fallback: handles remaining items

        // Register media image resolver
        builder.Services.AddSingleton<IAIUmbracoMediaResolver, AIUmbracoMediaResolver>();
        
        // AuditLog infrastructure
        // Note: IAIAuditLogRepository is registered by persistence layer
        services.AddSingleton<IAIAuditLogFactory, AIAuditLogFactory>();
        services.AddSingleton<IAIAuditLogService, AIAuditLogService>();

        // Background job for audit-log cleanup
        services.AddHostedService<AIAuditLogCleanupBackgroundJob>();

        // Analytics infrastructure
        // Note: IAIUsageRecordRepository and IAIUsageStatisticsRepository are registered by persistence layer
        services.AddSingleton<IAIUsageRecordFactory, AIUsageRecordFactory>();
        services.AddSingleton<IAIUsageRecordingService, AIUsageRecordingService>();
        services.AddSingleton<IAIUsageAggregationService, AIUsageAggregationService>();
        services.AddSingleton<IAIUsageAnalyticsService, AIUsageAnalyticsService>();

        // Background jobs for analytics aggregation and cleanup
        services.AddHostedService<AIUsageHourlyAggregationJob>();
        services.AddHostedService<AIUsageDailyRollupJob>();
        services.AddHostedService<AIUsageStatisticsCleanupJob>();

        // Register built-in test graders
        builder.AITestGraders()
            .Add<ExactMatchGrader>()
            .Add<ContainsGrader>()
            .Add<RegexGrader>()
            .Add<JSONSchemaGrader>()
            .Add<ToolCallGrader>()
            .Add<LLMJudgeGrader>()
            .Add<SemanticSimilarityGrader>();

        // Register test infrastructure services
        // Note: IAITestRepository and IAITestRunRepository are registered by persistence layer
        services.AddSingleton<IAITestRunner, AITestRunner>();
        services.AddSingleton<IAITestService, AITestService>();

        return builder;
    }
}
