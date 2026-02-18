using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Api.Management.TestRun.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Controller to get the latest test run for a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class LatestTestRunController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatestTestRunController"/> class.
    /// </summary>
    public LatestTestRunController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the most recent run for a test.
    /// </summary>
    /// <param name="testId">The test ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest test run.</returns>
    [HttpGet("latest/{testId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestRunResponseModel>> GetLatest(
        Guid testId,
        CancellationToken cancellationToken = default)
    {
        var run = await _runService.GetLatestRunAsync(testId, cancellationToken);

        if (run is null)
        {
            return NotFound(CreateProblemDetails("Test run not found", "No runs found for this test."));
        }

        var responseModel = _mapper.Map<TestRunResponseModel>(run)!;
        return Ok(responseModel);
    }
}
