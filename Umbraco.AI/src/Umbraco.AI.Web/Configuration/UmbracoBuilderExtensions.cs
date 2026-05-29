using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using Umbraco.AI.Web;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Mapping;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Mapping;
using Umbraco.AI.Web.Api.Management.AuditLog.Mapping;
using Umbraco.AI.Web.Api.Management.Chat.Mapping;
using Umbraco.AI.Web.Api.Management.Common.Mapping;
using Umbraco.AI.Web.Api.Management.Connection.Mapping;
using Umbraco.AI.Web.Api.Management.Context.Mapping;
using Umbraco.AI.Web.Api.Management.ContextResourceTypes.Mapping;
using Umbraco.AI.Web.Api.Management.Embedding.Mapping;
using Umbraco.AI.Web.Api.Management.Guardrail.Mapping;
using Umbraco.AI.Web.Api.Management.Profile.Mapping;
using Umbraco.AI.Web.Api.Management.Provider.Mapping;
using Umbraco.AI.Web.Api.Management.Settings.Mapping;
using Umbraco.AI.Web.Api.Management.Test.Mapping;
using Umbraco.AI.Web.Api.Management.Tool.Mapping;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Extensions;

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
    internal static IUmbracoBuilder AddUmbracoAIWeb(this IUmbracoBuilder builder)
    {
        builder.AddUmbracoAIManagementApi();
        builder.AddUmbracoAIMapDefinitions();

        // Security
        builder.Services.AddSingleton<IAuthorizationHandler, AISectionAuthorizationHandler>();
        builder.Services.AddAuthorization(o =>
        {
            o.AddPolicy(AIAuthorizationPolicies.SectionAccessAI, policy =>
            {
                policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.Requirements.Add(new AISectionRequirement());
            });
        });

        return builder;
    }

    private static IUmbracoBuilder AddUmbracoAIMapDefinitions(this IUmbracoBuilder builder)
    {
        builder.WithCollectionBuilder<MapDefinitionCollectionBuilder>()
            .Add<UsageDetailsMapDefinition>()
            .Add<CommonMapDefinition>()
            .Add<ConnectionMapDefinition>()
            .Add<ProfileMapDefinition>()
            .Add<ContextMapDefinition>()
            .Add<ContextResourceTypeMapDefinition>()
            .Add<ProviderMapDefinition>()
            .Add<EmbeddingMapDefinition>()
            .Add<ChatMapDefinition>()
            .Add<TestMapDefinition>()
            .Add<AuditLogMapDefinition>()
            .Add<UsageMapDefinition>()
            .Add<SettingsMapDefinition>()
            .Add<ToolMapDefinition>()
            .Add<GuardrailMapDefinition>();

        return builder;
    }

    private static IUmbracoBuilder AddUmbracoAIManagementApi(this IUmbracoBuilder builder)
        => builder.WithUmbracoAIManagementApi(
            Constants.ManagementApi.ApiName,
            Constants.ManagementApi.ApiTitle,
            $"Describes the {Constants.ManagementApi.ApiTitle} available for managing AI connections, profiles, and providers when authenticated as a backoffice user.");

    /// <summary>
    /// Registers an Umbraco AI Management API OpenAPI document.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <param name="apiName">The API name. Matches the <c>[MapToApi]</c> value on controllers (also used as the OpenAPI document name).</param>
    /// <param name="apiTitle">The OpenAPI document title.</param>
    /// <param name="apiDescription">Optional document description displayed in the OpenAPI document and Swagger UI.</param>
    /// <param name="configureOptions">Optional callback for downstream packages to add their own transformers or override defaults.</param>
    /// <param name="configureJson">Optional callback to customise the named JSON serializer options for this API.</param>
    /// <returns>The same Umbraco builder for chaining.</returns>
    /// <remarks>
    /// Each AI product (Core, Prompt, Agent, add-ons) registers its own document with its own unique
    /// <paramref name="apiName"/>; controllers are scoped to their document via <c>[MapToApi(apiName)]</c>.
    /// </remarks>
    public static IUmbracoBuilder WithUmbracoAIManagementApi(
        this IUmbracoBuilder builder,
        string apiName,
        string apiTitle,
        string? apiDescription = null,
        Action<OpenApiOptions>? configureOptions = null,
        Action<JsonSerializerOptions>? configureJson = null)
    {
        builder.AddJsonOptions(apiName, configureJson);

        builder.AddBackOfficeOpenApiDocument(apiName, document => document
            .WithTitle(apiTitle)
            .WithBackOfficeAuthentication()
            .WithJsonOptions(apiName)
            .ConfigureOpenApiOptions(options =>
            {
                if (string.IsNullOrWhiteSpace(apiDescription) == false)
                {
                    options.AddDocumentTransformer((doc, _, _) =>
                    {
                        doc.Info.Description = apiDescription;
                        doc.Info.Version = "Latest";
                        return Task.CompletedTask;
                    });
                }

                // Replaces the v17 UmbracoAIApiOperationIdHandler (action name, first letter lower-cased).
                options.AddOperationTransformer<UmbracoAIOperationIdTransformer>();

                // Replaces the v17 SseResponseOperationFilter (text/event-stream 200 response).
                options.AddOperationTransformer<SseResponseOperationTransformer>();

                // Microsoft.AspNetCore.OpenApi names derived polymorphic schemas as
                // {baseSchemaId}{derivedTypeName} (see dotnet/aspnetcore#58332).
                // CreateSchemaReferenceId can't intercept derived names, so this transformer
                // walks the finished document and shortens them back to {derivedTypeName} —
                // preserving v17 TypeScript client type names across the migration.
                options.AddDocumentTransformer<PreservePolymorphicSchemaNamesTransformer>();

                // Replaces the v17 UmbracoAIApiSchemaIdHandler. The default delegate (set above by
                // AddBackOfficeOpenApiDocument) only applies Umbraco's naming convention to Umbraco.Cms.*
                // types — types in our namespaces fall through to the framework default, which produces
                // different names than v17. Override here so Umbraco.AI.* types get the same convention,
                // preserving generated TypeScript client type names across the v17 -> v18 migration.
                var inheritedSchemaReferenceId = options.CreateSchemaReferenceId;
                options.CreateSchemaReferenceId = jsonTypeInfo =>
                    IsUmbracoAIType(jsonTypeInfo)
                        ? UmbracoSchemaIdGenerator.Generate(Nullable.GetUnderlyingType(jsonTypeInfo.Type) ?? jsonTypeInfo.Type)
                        : inheritedSchemaReferenceId(jsonTypeInfo);

                configureOptions?.Invoke(options);
            }));

        return builder;
    }

    private static bool IsUmbracoAIType(JsonTypeInfo jsonTypeInfo)
    {
        Type targetType = Nullable.GetUnderlyingType(jsonTypeInfo.Type) ?? jsonTypeInfo.Type;
        return targetType.Namespace?.StartsWith(Constants.AppNamespaceRoot) is true;
    }
}
