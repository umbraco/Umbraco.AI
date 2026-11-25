using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Web.Api;
using Umbraco.AI.Prompt.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Base controller for prompt-specific endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Prompt.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Prompt.RouteSegment)]
public abstract class PromptControllerBase : UmbracoAIPromptManagementControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a prompt.
    /// </summary>
    protected IActionResult PromptNotFound() => NotFound(new ProblemDetails
    {
        Title = "AIPrompt not found",
        Detail = "The specified prompt could not be found.",
        Status = StatusCodes.Status404NotFound
    });

    /// <summary>
    /// Returns a 403 Forbidden response for a prompt the caller may not use: it exists, but is limited to
    /// user groups the caller is not in.
    /// </summary>
    protected IActionResult PromptNotAvailable() => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Type = "Error",
        Title = "Prompt not available",
        Detail = "The specified prompt is not available to you.",
        Status = StatusCodes.Status403Forbidden
    });

    /// <summary>
    /// Returns a 403 Forbidden response with the reason a change was refused.
    /// </summary>
    protected IActionResult Forbidden(string detail) => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Type = "Error",
        Title = "Forbidden",
        Detail = detail,
        Status = StatusCodes.Status403Forbidden
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
