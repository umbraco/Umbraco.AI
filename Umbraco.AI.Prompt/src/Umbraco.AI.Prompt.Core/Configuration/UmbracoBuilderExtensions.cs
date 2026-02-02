using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Extensions;
using Umbraco.AI.Prompt.Core.Context;
using Umbraco.AI.Prompt.Core.Media;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Core.Templates.Processors;
using Umbraco.Cms.Core.DependencyInjection;

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
    public static IUmbracoBuilder AddUmbracoAiPromptCore(this IUmbracoBuilder builder)
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

        // Register service (Singleton to match IAIProfileService pattern and allow use in context resolvers)
        builder.Services.AddSingleton<IAIPromptService, AIPromptService>();

        // Register prompt context resolver
        builder.AIContextResolvers().Append<PromptContextResolver>();

        // Register versionable entity adapter for prompts
        builder.AIVersionableEntityAdapters().Add<AIPromptVersionableEntityAdapter>();

        return builder;
    }
}
