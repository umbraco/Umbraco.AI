using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Ai.Web;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Chat.Mapping;
using Umbraco.Ai.Web.Api.Management.Common.Mapping;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Connection.Mapping;
using Umbraco.Ai.Web.Api.Management.Context.Mapping;
using Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Mapping;
using Umbraco.Ai.Web.Api.Management.Embedding.Mapping;
using Umbraco.Ai.Web.Api.Management.Profile.Mapping;
using Umbraco.Ai.Web.Api.Management.Provider.Mapping;
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
            .Add<ContextMapDefinition>()
            .Add<ContextResourceTypeMapDefinition>()
            .Add<ProviderMapDefinition>()
            .Add<EmbeddingMapDefinition>()
            .Add<ChatMapDefinition>();

        return builder;
    }

    private static IUmbracoBuilder AddUmbracoAiManagementApi(this IUmbracoBuilder builder)
    {
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

    /// <summary>
    /// Configures an Umbraco AI Management API with optional Swagger configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="apiName"></param>
    /// <param name="configureSwagger"></param>
    /// <param name="configureJson"></param>
    /// <returns></returns>
    public static IUmbracoBuilder WithUmbracoAiManagementApi(this IUmbracoBuilder builder, string apiName, Action<SwaggerGenOptions>? configureSwagger = null,
        Action<JsonSerializerOptions>? configureJson = null)
    {
        if (configureSwagger != null)
        {
            builder.Services.Configure<SwaggerGenOptions>(options =>
            {
                // Only add the swagger doc if it hasn't been added already
                if (options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(apiName))
                    return;

                configureSwagger(options);
                
                options.DocumentFilter<MimeTypeDocumentFilter>(apiName);
                options.OperationFilter<UmbracoAiManagementApiBackOfficeSecurityRequirementsOperationFilter>(apiName);
                options.OperationFilter<SwaggerOperationFilter>(apiName);
                options.OperationFilter<SseResponseOperationFilter>(apiName);
                
                // Map IdOrAlias to string in OpenAPI schema for cleaner client generation
                if (!options.SchemaGeneratorOptions.CustomTypeMappings.ContainsKey(typeof(IdOrAlias)))
                {
                    options.MapType<IdOrAlias>(() => new OpenApiSchema { Type = JsonSchemaType.String });
                }

                // Map System.Type to string in OpenAPI schema (JsonStringTypeConverter handles serialization)
                if (!options.SchemaGeneratorOptions.CustomTypeMappings.ContainsKey(typeof(Type)))
                {
                    options.MapType<Type>(() => new OpenApiSchema { Type = JsonSchemaType.String });
                }
            });
        }

        builder.Services.AddSingleton<IOperationIdHandler, UmbracoAiApiOperationIdHandler>();
        builder.Services.AddSingleton<ISchemaIdHandler, UmbracoAiApiSchemaIdHandler>();
        
        builder.AddJsonOptions(apiName, configureJson);
        
        return builder;
    }
}