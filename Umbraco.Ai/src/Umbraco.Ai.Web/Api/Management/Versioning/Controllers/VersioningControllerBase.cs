using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Versioning.Controllers;

/// <summary>
/// Base controller for Version History management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Versioning.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Versioning.RouteSegment)]
public abstract class VersioningControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a version.
    /// </summary>
    protected IActionResult VersionNotFound(int version)
        => NotFound(CreateProblemDetails(
            "Version not found",
            $"Version {version} was not found."));

    /// <summary>
    /// Returns a 400 Bad Request response for an unknown entity type.
    /// </summary>
    protected IActionResult UnknownEntityType(string entityType)
        => BadRequest(CreateProblemDetails(
            "Unknown entity type",
            $"Entity type '{entityType}' is not supported."));

    /// <summary>
    /// Returns a 404 Not Found response for an entity.
    /// </summary>
    protected IActionResult EntityNotFound(string entityType, Guid entityId)
        => NotFound(CreateProblemDetails(
            "Entity not found",
            $"Entity of type '{entityType}' with ID '{entityId}' was not found."));
}
