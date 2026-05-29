using Umbraco.AI.Extensions;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Mapping;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Prompt.Web.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.AI.Prompt.Web services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.AI.Prompt web services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAIPromptWeb(this IUmbracoBuilder builder)
    {
        // Register map definitions
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<PromptMapDefinition>()
            .Add<PromptExecutionMapDefinition>();

        // Participate in the shared Umbraco AI Management API document. The core Umbraco.AI.Web caller
        // registers the document; this call ensures named JSON options are applied for our controllers.
        builder.WithUmbracoAIManagementApi(
            Constants.ManagementApi.ApiName,
            Constants.ManagementApi.ApiTitle,
            $"Describes the {Constants.ManagementApi.ApiTitle} available for managing AI connections, profiles, and providers when authenticated as a backoffice user.");

        return builder;
    }
}
