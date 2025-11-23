using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Ai.Cms.Api.Management.Api;
using Umbraco.Ai.Cms.Api.Management.Api.Management.Configuration;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

public static class UmbracoBuilderExtensions
{
    internal static IUmbracoBuilder AddUmbracoAiWeb(this IUmbracoBuilder builder)
    {
        builder.AddUmbracoAiManagementApi();
        
        return builder;
    }

    private static IUmbracoBuilder AddUmbracoAiManagementApi(this IUmbracoBuilder builder)
    {
        // Generate Swagger documentation for the management API
        builder.Services.Configure<SwaggerGenOptions>(options =>
        {
            options.SwaggerDoc(
                Constants.ManagementApi.ApiName,
                new OpenApiInfo
                {
                    Title = Constants.ManagementApi.ApiTitle,
                    Version = "Latest",
                    Description = $"Describes the {Constants.ManagementApi.ApiTitle} available for managing forms data when authenticated as a backoffice user.",
                });

            options.DocumentFilter<MimeTypeDocumentFilter>(Constants.ManagementApi.ApiTitle);
            options.OperationFilter<UmbracoAiManagementApiBackOfficeSecurityRequirementsOperationFilter>();
            // options.OperationFilter<SwaggerParameterAttributeFilter>();
        });

        builder.Services.AddSingleton<ISchemaIdHandler, UmbracoAiManagementApiSchemaIdHandler>();
        builder.Services.AddSingleton<IOperationIdHandler, UmbracoAiManagementApiOperationIdHandler>();

        return builder;
    }
}