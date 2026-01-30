using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Tests;
using Umbraco.Ai.Web.Api.Management.Test.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get the latest test run for a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class LatestTestRunController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public LatestTestRunController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get the latest run for a specific test.
    /// </summary>
    [HttpGet("runs/latest")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatest(
        [FromQuery] Guid testId,
        CancellationToken cancellationToken = default)
    {
        // Verify test exists
        var test = await _testService.GetTestAsync(testId, cancellationToken);
        if (test is null)
        {
            return TestNotFound();
        }

        var run = await _testService.GetLatestRunAsync(testId, cancellationToken);
        if (run is null)
        {
            return TestRunNotFound();
        }

        return Ok(_mapper.Map<TestRunResponseModel>(run));
    }
}
