using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Base controller for Guardrail management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Guardrail.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Guardrail.RouteSegment)]
public abstract class GuardrailControllerBase : UmbracoAICoreManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a guardrail.
    /// </summary>
    protected IActionResult GuardrailNotFound()
        => OperationStatusResult(GuardrailOperationStatus.NotFound, problemDetailsBuilder
            => NotFound(problemDetailsBuilder
                .WithTitle("Guardrail not found")
                .WithDetail("The specified guardrail could not be found.")
                .Build()));

    /// <summary>
    /// Maps a guardrail operation status to an appropriate action result.
    /// </summary>
    protected IActionResult GuardrailOperationStatusResult(GuardrailOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            GuardrailOperationStatus.NotFound => GuardrailNotFound(),
            GuardrailOperationStatus.DuplicateAlias => BadRequest(problemDetailsBuilder
                .WithTitle("Duplicate alias")
                .WithDetail("A guardrail with this alias already exists.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown guardrail operation status")
                .Build())
        });
}
