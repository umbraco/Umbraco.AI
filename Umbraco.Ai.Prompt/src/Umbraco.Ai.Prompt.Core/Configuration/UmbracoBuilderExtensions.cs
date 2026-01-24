using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Prompt.Core.Context;
using Umbraco.Ai.Prompt.Core.Media;
using Umbraco.Ai.Prompt.Core.Models;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Core.Templates.Processors;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Prompt.Core.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.Ai.Prompt.Core services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.Ai.Prompt core services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiPromptCore(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAiPromptService)))
        {
            return builder;
        }

        // Bind configuration
        builder.Services.Configure<AiPromptOptions>(
            builder.Config.GetSection(AiPromptOptions.SectionName));

        // Register in-memory repository as fallback (replaced by persistence layer)
        builder.Services.AddSingleton<IAiPromptRepository, InMemoryAiPromptRepository>();

        // Register template variable processors
        builder.Services.AddSingleton<TextTemplateVariableProcessor>();
        builder.Services.AddSingleton<ImageTemplateVariableProcessor>();

        // Register template service
        builder.Services.AddSingleton<IAiPromptTemplateService, AiPromptTemplateService>();

        // Register scope validator
        builder.Services.AddScoped<IAiPromptScopeValidator, AiPromptScopeValidator>();

        // Register service (Singleton to match IAiProfileService pattern and allow use in context resolvers)
        builder.Services.AddSingleton<IAiPromptService, AiPromptService>();

        // Register prompt context resolver
        builder.AiContextResolvers().Append<PromptContextResolver>();

        // Register versionable entity adapter for prompts
        builder.AiVersionableEntityAdapters().Add<AiPromptVersionableEntityAdapter>();

        return builder;
    }
}
