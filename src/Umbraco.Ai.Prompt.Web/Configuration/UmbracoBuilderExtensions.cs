using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Mapping;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Prompt.Web.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.Ai.Prompt.Web services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.Ai.Prompt web services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiPromptWeb(this IUmbracoBuilder builder)
    {
        // Register map definitions
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<PromptMapDefinition>();

        return builder;
    }
}
