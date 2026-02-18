using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.TestRun.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Controller to compare two test runs for regression detection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class CompareRunsController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareRunsController"/> class.
    /// </summary>
    public CompareRunsController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Compares two test runs and detects regressions.
    /// </summary>
    /// <param name="requestModel">The comparison request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison result with regression detection.</returns>
    [HttpPost("compare")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestRunComparisonResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestRunComparisonResponseModel>> CompareRuns(
        [FromBody] CompareRunsRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comparison = await _runService.CompareRunsAsync(
                requestModel.BaselineRunId,
                requestModel.ComparisonRunId,
                cancellationToken);

            var responseModel = _mapper.Map<TestRunComparisonResponseModel>(comparison)!;
            return Ok(responseModel);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Bad request", ex.Message));
        }
    }
}
