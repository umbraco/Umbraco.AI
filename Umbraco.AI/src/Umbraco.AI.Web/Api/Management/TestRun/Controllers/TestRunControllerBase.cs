using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Base controller for test run endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Test.GroupName)]
[UmbracoAIVersionedManagementApiRoute("test-runs")]
public abstract class TestRunControllerBase : UmbracoAICoreManagementControllerBase
{
    /// <summary>
    /// Creates a NotFound problem details response.
    /// </summary>
    protected IActionResult TestRunNotFound() =>
        NotFound(CreateProblemDetails("Test run not found", "The requested test run could not be found."));

    /// <summary>
    /// Creates a BadRequest problem details response.
    /// </summary>
    protected IActionResult TestRunBadRequest(string detail) =>
        BadRequest(CreateProblemDetails("Bad request", detail));
}
