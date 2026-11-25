using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Media;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Extensions;
using Umbraco.AI.Prompt.Core.Context;
using Umbraco.AI.Prompt.Core.Guardrails;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Core.Telemetry;
using Umbraco.AI.Prompt.Core.Templates.Processors;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Prompt.Core.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.AI.Prompt.Core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.AI.Prompt core services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIPromptCore(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAIPromptService)))
        {
            return builder;
        }

        // Bind configuration
        builder.Services.Configure<AIPromptOptions>(
            builder.Config.GetSection(AIPromptOptions.SectionName));

        // Register in-memory repository as fallback (replaced by persistence layer)
        builder.Services.AddSingleton<IAIPromptRepository, InMemoryAIPromptRepository>();

        // Register template variable processors
        builder.Services.AddSingleton<TextTemplateVariableProcessor>();
        builder.Services.AddSingleton<ImageTemplateVariableProcessor>();

        // Register template service
        builder.Services.AddSingleton<IAIPromptTemplateService, AIPromptTemplateService>();

        // Register scope validator
        builder.Services.AddScoped<IAIPromptScopeValidator, AIPromptScopeValidator>();

        // Register property value schema resolver (backs schema-driven generation for non-string editors)
        builder.Services.AddSingleton<IAIPromptPropertyValueSchemaResolver, AIPromptPropertyValueSchemaResolver>();

        // Register service (Singleton to match IAIProfileService pattern and allow use in context resolvers)
        // One instance behind both: the limited IAIPromptService for request paths, and the internal
        // unrestricted reader for server-side code that runs after an execution was already authorized.
        builder.Services.AddSingleton<AIPromptService>();
        builder.Services.AddSingleton<IAIPromptService>(sp => sp.GetRequiredService<AIPromptService>());
        builder.Services.AddSingleton<IAIPromptUnrestrictedReader>(sp => sp.GetRequiredService<AIPromptService>());
        builder.Services.AddSingleton<IAIPromptUnrestrictedWriter>(sp => sp.GetRequiredService<AIPromptService>());

        // Prevent deletion of profiles referenced by prompts
        builder.AddNotificationAsyncHandler<AIProfileDeletingNotification, AIProfileDeletingPromptNotificationHandler>();

        // Register prompt context resolver
        builder.AIContextResolvers().Append<PromptContextResolver>();

        // Register prompt guardrail resolver (runs after profile resolver)
        builder.AIGuardrailResolvers().Append<PromptGuardrailResolver>();

        // Register versionable entity adapter for prompts
        builder.AIVersionableEntityAdapters().Add<AIPromptVersionableEntityAdapter>();

        // Usage telemetry - contributes anonymous aggregate counts to the CMS telemetry report
        builder.Services.AddTransient<IDetailedTelemetryProvider, AIPromptUsageTelemetryProvider>();

        return builder;
    }
}
