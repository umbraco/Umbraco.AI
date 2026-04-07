using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Persistence;
using Umbraco.AI.Persistence.Analytics;
using Umbraco.AI.Persistence.Analytics.Usage;
using Umbraco.AI.Persistence.Connections;
using Umbraco.AI.Persistence.Context;
using Umbraco.AI.Persistence.Guardrails;
using Umbraco.AI.Persistence.AuditLog;
using Umbraco.AI.Persistence.Notifications;
using Umbraco.AI.Persistence.Profiles;
using Umbraco.AI.Persistence.Settings;
using Umbraco.AI.Persistence.Tests;
using Umbraco.AI.Persistence.Versioning;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for configuring Umbraco AI persistence.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds EF Core persistence for Umbraco AI.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIPersistence(this IUmbracoBuilder builder)
    {
        // Resolve AI connection string upfront (falls back to Umbraco CMS connection)
        var (aiConnectionString, aiProviderName) = AIConnectionStringResolver.Resolve(builder.Config);

        builder.Services.AddUmbracoDbContext<UmbracoAIDbContext>((options, connectionString, providerName, serviceProvider) =>
        {
            UmbracoAIDbContext.ConfigureProvider(options, aiConnectionString ?? connectionString, aiProviderName ?? providerName);
        });

        // Connection factory for entity/domain mapping with encryption support
        builder.Services.AddSingleton<IAIConnectionFactory, AIConnectionFactory>();

        // Replace in-memory repository with EF Core implementations (Singleton - IEFCoreScopeProvider manages scopes internally)
        builder.Services.AddSingleton<IAIConnectionRepository, EFCoreAIConnectionRepository>();
        builder.Services.AddSingleton<IAIProfileRepository, EFCoreAIProfileRepository>();
        builder.Services.AddSingleton<IAIContextRepository, EFCoreAIContextRepository>();
        builder.Services.AddSingleton<IAIGuardrailRepository, EFCoreAIGuardrailRepository>();
        builder.Services.AddSingleton<IAIAuditLogRepository, EFCoreAIAuditLogRepository>();
        builder.Services.AddSingleton<IAIUsageRecordRepository, EFCoreAIUsageRecordRepository>();
        builder.Services.AddSingleton<IAIUsageStatisticsRepository, EFCoreAIUsageStatisticsRepository>();
        builder.Services.AddSingleton<IAISettingsRepository, EFCoreAISettingsRepository>();

        // Unified versioning repository
        builder.Services.AddSingleton<IAIEntityVersionRepository, EFCoreAIEntityVersionRepository>();

        // Test factory for entity/domain mapping with encryption support
        builder.Services.AddSingleton<IAITestFactory, AITestFactory>();

        // Test framework repositories
        builder.Services.AddSingleton<IAITestRepository, EFCoreAITestRepository>();
        builder.Services.AddSingleton<IAITestRunRepository, EFCoreAITestRunRepository>();
        builder.Services.AddSingleton<IAITestTranscriptRepository, EFCoreAITestTranscriptRepository>();

        // Register migration notification handler
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAIMigrationNotificationHandler>();

        return builder;
    }

}
