using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Prompt.Core.Models;
using Umbraco.Ai.Prompt.Core.Prompts;
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

        // Register template service
        builder.Services.AddSingleton<IAiPromptTemplateService, AiPromptTemplateService>();

        // Register service
        builder.Services.AddScoped<IAiPromptService, AiPromptService>();

        return builder;
    }
}
