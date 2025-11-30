using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Json;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Chat.Mapping;
using Umbraco.Ai.Web.Api.Management.Common.Mapping;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Connection.Mapping;
using Umbraco.Ai.Web.Api.Management.Embedding.Mapping;
using Umbraco.Ai.Web.Api.Management.Profile.Mapping;
using Umbraco.Ai.Web.Api.Management.Provider.Mapping;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for configuring Umbraco AI web services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds Umbraco AI web services including the Management API.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The Umbraco builder for chaining.</returns>
    internal static IUmbracoBuilder AddUmbracoAiWeb(this IUmbracoBuilder builder)
    {
        builder.AddJsonOptions(Constants.ManagementApi.ApiName);
        builder.AddUmbracoAiManagementApi();
        builder.AddUmbracoAiMapDefinitions();

        return builder;
    }

    private static IUmbracoBuilder AddUmbracoAiMapDefinitions(this IUmbracoBuilder builder)
    {
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<CommonMapDefinition>()
            .Add<ConnectionMapDefinition>()
            .Add<ProfileMapDefinition>()
            .Add<ProviderMapDefinition>()
            .Add<EmbeddingMapDefinition>()
            .Add<ChatMapDefinition>();

        return builder;
    }

    private static IUmbracoBuilder AddUmbracoAiManagementApi(this IUmbracoBuilder builder)
    {
        // Generate Swagger documentation for the management API
        builder.Services.Configure<SwaggerGenOptions>(options =>
        {
            // Only add the swagger doc if it hasn't been added already
            if (options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(Constants.ManagementApi.ApiName))
                return;

            options.SwaggerDoc(
                Constants.ManagementApi.ApiName,
                new OpenApiInfo
                {
                    Title = Constants.ManagementApi.ApiTitle,
                    Version = "Latest",
                    Description = $"Describes the {Constants.ManagementApi.ApiTitle} available for managing AI connections, profiles, and providers when authenticated as a backoffice user."
                });

            options.DocumentFilter<MimeTypeDocumentFilter>(Constants.ManagementApi.ApiTitle);
            options.OperationFilter<UmbracoAiManagementApiBackOfficeSecurityRequirementsOperationFilter>();
            options.OperationFilter<SwaggerOperationFilter>(Constants.ManagementApi.ApiName);

            // Map IdOrAlias to string in OpenAPI schema for cleaner client generation
            if (!options.SchemaGeneratorOptions.CustomTypeMappings.ContainsKey(typeof(IdOrAlias)))
            {
                options.MapType<IdOrAlias>(() => new OpenApiSchema { Type = JsonSchemaType.String });
            }
        });

        builder.Services.AddSingleton<IOperationIdHandler, UmbracoAiApiOperationIdHandler>();
        builder.Services.AddSingleton<ISchemaIdHandler, UmbracoAiApiSchemaIdHandler>();

        return builder;
    }
}