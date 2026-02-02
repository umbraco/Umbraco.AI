using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Mapping;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Agent.Web.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco.AI.Agent.Web services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco.AI.Agent web services to the builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IUmbracoBuilder AddUmbracoAiAgentWeb(this IUmbracoBuilder builder)
    {
        // Register map definitions
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<AgentMapDefinition>(); 

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
