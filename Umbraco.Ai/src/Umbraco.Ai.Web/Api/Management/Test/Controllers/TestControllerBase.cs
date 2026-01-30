using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Test.Controllers;

/// <summary>
/// Base controller for Test management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = "umbraco-ai-test")]
[UmbracoAiVersionedManagementApiRoute("test")]
public abstract class TestControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Returns a not found result for a test.
    /// </summary>
    protected IActionResult TestNotFound() => NotFound(ProblemDetailsBuilder()
        .WithTitle("Test not found")
        .WithDetail("The specified test does not exist.")
        .Build());

    /// <summary>
    /// Returns a not found result for a test run.
    /// </summary>
    protected IActionResult TestRunNotFound() => NotFound(ProblemDetailsBuilder()
        .WithTitle("Test run not found")
        .WithDetail("The specified test run does not exist.")
        .Build());

    /// <summary>
    /// Returns a not found result for a test feature.
    /// </summary>
    protected IActionResult TestFeatureNotFound() => NotFound(ProblemDetailsBuilder()
        .WithTitle("Test feature not found")
        .WithDetail("The specified test feature does not exist.")
        .Build());

    /// <summary>
    /// Returns a bad request result for duplicate alias.
    /// </summary>
    protected IActionResult DuplicateAliasResult() => BadRequest(ProblemDetailsBuilder()
        .WithTitle("Duplicate alias")
        .WithDetail("A test with this alias already exists.")
        .Build());
}
