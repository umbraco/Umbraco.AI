using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Base controller for Context management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Context.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Context.RouteSegment)]
public abstract class ContextControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Maps a context operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult ContextOperationStatusResult(ContextOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            ContextOperationStatus.NotFound => ContextNotFound(),
            ContextOperationStatus.DuplicateAlias => BadRequest(problemDetailsBuilder
                .WithTitle("Duplicate alias")
                .WithDetail("A context with this alias already exists.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown context operation status")
                .Build())
        });
}
