using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Umbraco.Ai.Agent.Web.Api.Management.Prompt.Mapping;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Agent.Web.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.Ai.Agent.Web services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.Ai.Agent web services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiAgentWeb(this IUmbracoBuilder builder)
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
