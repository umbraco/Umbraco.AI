using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Prompt.Core.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Persistence.Configuration;
using Umbraco.AI.Prompt.Web.Configuration;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Prompt.Extensions;

/// <summary>
/// Extension methods for adding all Umbraco.AI.Prompt services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds all Umbraco.AI.Prompt services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIPrompt(this IUmbracoBuilder builder)
    {
        // Prevent multiple registrations
        if (builder.Services.Any(x => x.ServiceType == typeof(IAIPromptService)))
        {
            return builder;
        }

        builder.AddUmbracoAIPromptCore();
        builder.AddUmbracoAIPromptPersistence();
        builder.AddUmbracoAIPromptWeb();

        return builder;
    }
}
