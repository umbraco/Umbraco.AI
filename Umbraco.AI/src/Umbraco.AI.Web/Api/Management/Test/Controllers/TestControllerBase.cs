using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Base controller for Test management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Test.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Test.RouteSegment)]
public abstract class TestControllerBase : UmbracoAICoreManagementControllerBase
{
    /// <summary>
    /// Maps a test operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult TestOperationStatusResult(TestOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            TestOperationStatus.NotFound => TestNotFound(),
            TestOperationStatus.DuplicateAlias => BadRequest(problemDetailsBuilder
                .WithTitle("Duplicate alias")
                .WithDetail("A test with this alias already exists.")
                .Build()),
            TestOperationStatus.InvalidTestType => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid test type")
                .WithDetail("The specified test type does not exist.")
                .Build()),
            TestOperationStatus.InvalidTarget => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid target")
                .WithDetail("The specified target is not valid.")
                .Build()),
            TestOperationStatus.InvalidRunCount => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid run count")
                .WithDetail("Run count must be at least 1.")
                .Build()),
            TestOperationStatus.InvalidTestCase => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid test case")
                .WithDetail("Test case JSON cannot be empty.")
                .Build()),
            TestOperationStatus.Cancelled => BadRequest(problemDetailsBuilder
                .WithTitle("Operation cancelled")
                .WithDetail("The operation was cancelled by a notification handler.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown test operation status")
                .Build())
        });

    /// <summary>
    /// Creates a NotFound result for a test.
    /// </summary>
    protected IActionResult TestNotFound() =>
        NotFound(CreateProblemDetails("Test not found", "The requested test could not be found."));
}
