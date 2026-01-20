using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Web.Api;
using Umbraco.Ai.Prompt.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Base controller for prompt-specific endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Prompt.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Prompt.RouteSegment)]
public abstract class PromptControllerBase : UmbracoAiPromptManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a prompt.
    /// </summary>
    protected IActionResult PromptNotFound() => NotFound(new ProblemDetails
    {
        Title = "AiPrompt not found",
        Detail = "The specified prompt could not be found.",
        Status = StatusCodes.Status404NotFound
    });

    /// <summary>
    /// Returns a 409 Conflict response for duplicate alias.
    /// </summary>
    protected IActionResult AliasAlreadyExists(string alias) => Conflict(new ProblemDetails
    {
        Title = "Alias already exists",
        Detail = $"A prompt with alias '{alias}' already exists.",
        Status = StatusCodes.Status409Conflict
    });
}
