using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Context.Controllers;

/// <summary>
/// Base controller for Context management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Context.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Context.RouteSegment)]
public abstract class ContextControllerBase : UmbracoAICoreManagementControllerBase
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
