using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Filters;
using UmbracoAiConstants = Umbraco.Ai.Web.Api.Constants;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco AI Prompt Management API controllers.
/// Uses the same Swagger API group as Umbraco.Ai.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[MapToApi(UmbracoAiConstants.ManagementApi.ApiName)] // "ai-management" - shared with Umbraco.Ai
[JsonOptionsName(UmbracoAiConstants.ManagementApi.ApiName)]
[DisableBrowserCache]
[Produces("application/json")]
public abstract class UmbracoAiPromptManagementControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a 404 Not Found response for a prompt.
    /// </summary>
    protected IActionResult PromptNotFound() => NotFound(new ProblemDetails
    {
        Title = "Prompt not found",
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
