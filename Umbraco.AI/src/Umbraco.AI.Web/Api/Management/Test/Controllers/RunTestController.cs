using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to run a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class RunTestController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunTestController"/> class.
    /// </summary>
    public RunTestController(IAITestService testService, IUmbracoMapper umbracoMapper)
    {
        _testService = testService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Execute a test and get the run result.
    /// Creates a new test run and executes the test N times (based on test.RunCount).
    /// Returns the full run result with transcripts, outcomes, and pass@k metrics.
    /// </summary>
    /// <param name="idOrAlias">The test ID or alias.</param>
    /// <param name="requestModel">Optional overrides for profile and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test run result.</returns>
    [HttpPost("{idOrAlias}/run")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestRunResponseModel>> RunTest(
        string idOrAlias,
        [FromBody] RunTestRequestModel? requestModel,
        CancellationToken cancellationToken = default)
    {
        // Find existing test
        AITest? existing = null;
        if (Guid.TryParse(idOrAlias, out var id))
        {
            existing = await _testService.GetTestAsync(id, cancellationToken);
        }
        if (existing is null)
        {
            existing = await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);
        }
        if (existing is null)
        {
            return TestNotFound();
        }

        // Execute the test
        var testRun = await _testService.RunTestAsync(
            existing.Id,
            requestModel?.ProfileIdOverride,
            requestModel?.ContextIdsOverride,
            cancellationToken);

        var responseModel = _umbracoMapper.Map<TestRunResponseModel>(testRun)!;
        return Ok(responseModel);
    }
}
