using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.AuditLog.Controllers;

/// <summary>
/// Base controller for AuditLog management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.AuditLog.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.AuditLog.RouteSegment)]
public abstract class AuditLogControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Maps a audit-log operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult AuditLogOperationStatusResult(AuditLogOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            AuditLogOperationStatus.NotFound => NotFound(problemDetailsBuilder
                .WithTitle("AuditLog not found")
                .WithDetail("The specified audit-log does not exist.")
                .Build()),
            AuditLogOperationStatus.InvalidFilter => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid filter")
                .WithDetail("The specified filter criteria are invalid.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown audit-log operation status")
                .Build())
        });
}

/// <summary>
/// Represents the possible operation statuses for audit-log operations.
/// </summary>
public enum AuditLogOperationStatus
{
    /// <summary>
    /// AuditLog not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Invalid filter criteria provided.
    /// </summary>
    InvalidFilter
}
