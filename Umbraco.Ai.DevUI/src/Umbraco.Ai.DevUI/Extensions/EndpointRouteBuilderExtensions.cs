using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Ai.DevUI.Models;
using Umbraco.Ai.DevUI.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.DevUI.Extensions;

/// <summary>
/// Extension methods for mapping DevUI endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all DevUI endpoints including the frontend, meta API, and custom entity discovery.
    /// </summary>
    public static IEndpointRouteBuilder MapUmbracoAiDevUI(this IEndpointRouteBuilder endpoints)
    {
        // Map DevUI components individually using reflection (skip MapEntities to avoid conflicts)
        MapDevUIComponentsWithoutEntities(endpoints);

        // Map our custom entity discovery endpoints with runtime agent support
        MapUmbracoAgentDiscovery(endpoints);

        return endpoints;
    }

    /// <summary>
    /// Uses reflection to call DevUI's internal mapping methods (skip MapEntities to avoid conflicts).
    /// </summary>
    private static void MapDevUIComponentsWithoutEntities(IEndpointRouteBuilder app)
    {
        var logger = app.ServiceProvider.GetRequiredService<ILogger<IDevUIEntityDiscoveryService>>();
        var devUIAssembly = typeof(Microsoft.Agents.AI.DevUI.DevUIExtensions).Assembly;

        // Find DevUIExtensions type which has the internal MapDevUI(pattern) method
        var devUIExtensionsType = devUIAssembly.GetType("Microsoft.Agents.AI.DevUI.DevUIExtensions");
        if (devUIExtensionsType == null)
            throw new InvalidOperationException("Could not find DevUIExtensions type");

        // Call internal MapDevUI(endpoints, "/devui") to map the frontend
        var mapDevUIMethod = devUIExtensionsType.GetMethod(
            "MapDevUI",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
            null,
            [typeof(IEndpointRouteBuilder), typeof(string)],
            null);

        if (mapDevUIMethod != null)
        {
            logger.LogInformation("Mapping DevUI frontend at /umbraco/devui");
            mapDevUIMethod.Invoke(null, [app, "/umbraco/devui"]);
        }
        else
        {
            logger.LogWarning("Could not find MapDevUI method");
        }

        // Find MetaApiExtensions type
        var metaApiExtensionsType = devUIAssembly.GetType("Microsoft.Agents.AI.DevUI.MetaApiExtensions");
        if (metaApiExtensionsType == null)
            throw new InvalidOperationException("Could not find MetaApiExtensions type");

        // Call MapMeta() to map the /meta endpoint
        var mapMetaMethod = metaApiExtensionsType.GetMethod(
            "MapMeta",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            [typeof(IEndpointRouteBuilder)],
            null);

        if (mapMetaMethod != null)
        {
            logger.LogInformation("Mapping DevUI meta endpoint at /meta");
            mapMetaMethod.Invoke(null, [app]);
        }
        else
        {
            logger.LogWarning("Could not find MapMeta method");
        }

        logger.LogInformation("DevUI components mapped successfully");
    }

    /// <summary>
    /// Maps custom discovery endpoints that query Umbraco.Ai agents at runtime.
    /// All endpoints require backoffice authentication.
    /// </summary>
    private static void MapUmbracoAgentDiscovery(IEndpointRouteBuilder endpoints)
    {
        // Map directly to /v1/entities routes to override MapDevUI's default endpoints
        // Override GET /v1/entities to include runtime Umbraco agents
        endpoints.MapGet("/v1/entities", async (
            IDevUIEntityDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var response = await discoveryService.GetAllEntitiesAsync(ct);
            return Results.Json(response, DevUIJsonSerializerOptions.Options);
        })
        .RequireAuthorization(AuthorizationPolicies.BackOfficeAccess)
        .Produces<DevUIDiscoveryResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .ExcludeFromDescription(); // Don't show in Swagger since MapDevUI will also document this route

        // Override GET /v1/entities/{entityId}/info for individual agent details
        endpoints.MapGet("/v1/entities/{entityId}/info", async (
            string entityId,
            IDevUIEntityDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var entityInfo = await discoveryService.GetEntityInfoAsync(entityId, ct);
            return entityInfo != null
                ? Results.Json(entityInfo, DevUIJsonSerializerOptions.Options)
                : Results.NotFound();
        })
        .RequireAuthorization(AuthorizationPolicies.BackOfficeAccess)
        .Produces<DevUIEntityInfo>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .ExcludeFromDescription(); // Don't show in Swagger since MapDevUI will also document this route
    }
}
