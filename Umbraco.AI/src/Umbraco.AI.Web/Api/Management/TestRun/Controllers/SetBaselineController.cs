using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Controller to set a test run as the baseline for comparisons.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class SetBaselineController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetBaselineController"/> class.
    /// </summary>
    public SetBaselineController(IAITestRunService runService)
    {
        _runService = runService;
    }

    /// <summary>
    /// Sets a run as the baseline for future comparisons.
    /// The baseline run is used as the reference point when detecting regressions.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="runId">The run ID to set as baseline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("baseline/{testId:guid}/{runId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBaseline(
        Guid testId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var success = await _runService.SetBaselineRunAsync(testId, runId, cancellationToken);

        if (!success)
        {
            return NotFound(CreateProblemDetails(
                "Not found",
                "The test or run was not found, or the run does not belong to the specified test."));
        }

        return NoContent();
    }
}
