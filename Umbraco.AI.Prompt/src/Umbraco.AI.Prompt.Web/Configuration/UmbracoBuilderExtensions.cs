using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Mapping;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Prompt.Web.Configuration;

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
            .Add<PromptMapDefinition>()
            .Add<PromptExecutionMapDefinition>(); 

        // Configure Management API
        builder.WithUmbracoAiManagementApi(Constants.ManagementApi.ApiName, options =>
        {
            options.SwaggerDoc(
                Constants.ManagementApi.ApiName,
                new OpenApiInfo
                {
                    Title = Constants.ManagementApi.ApiTitle,
                    Version = "Latest",
                    Description = $"Describes the {Constants.ManagementApi.ApiTitle} available for managing AI connections, profiles, and providers when authenticated as a backoffice user."
                });
        });
        
        return builder;
    }
}
