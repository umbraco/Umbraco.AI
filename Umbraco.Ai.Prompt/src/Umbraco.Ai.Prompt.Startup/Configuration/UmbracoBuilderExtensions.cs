using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Prompt.Core.Configuration;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Persistence.Configuration;
using Umbraco.Ai.Prompt.Web.Configuration;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Prompt.Extensions;

/// <summary>
/// Extension methods for adding all Umbraco.Ai.Prompt services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds all Umbraco.Ai.Prompt services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiPrompt(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAiPromptService)))
        {
            return builder;
        }

        builder.AddUmbracoAiPromptCore();
        builder.AddUmbracoAiPromptPersistence();
        builder.AddUmbracoAiPromptWeb();

        return builder;
    }
}
