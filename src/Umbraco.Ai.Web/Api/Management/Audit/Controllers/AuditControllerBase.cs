using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Base controller for Audit management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Audit.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Audit.RouteSegment)]
public abstract class AuditControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Maps a audit operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult AuditOperationStatusResult(AuditOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            AuditOperationStatus.NotFound => NotFound(problemDetailsBuilder
                .WithTitle("Audit not found")
                .WithDetail("The specified audit does not exist.")
                .Build()),
            AuditOperationStatus.InvalidFilter => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid filter")
                .WithDetail("The specified filter criteria are invalid.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown audit operation status")
                .Build())
        });
}

/// <summary>
/// Represents the possible operation statuses for audit operations.
/// </summary>
public enum AuditOperationStatus
{
    /// <summary>
    /// Audit not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Invalid filter criteria provided.
    /// </summary>
    InvalidFilter
}
