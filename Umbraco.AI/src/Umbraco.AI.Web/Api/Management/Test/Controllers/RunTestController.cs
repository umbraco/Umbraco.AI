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
    /// Execute a test and get the metrics result.
    /// Executes the test N times (based on test.RunCount) and calculates pass@k metrics.
    /// Returns metrics including total/passed runs, pass@k, pass^k, and run IDs.
    /// Use the returned run IDs to retrieve individual run details via the test run endpoints.
    /// </summary>
    /// <param name="idOrAlias">The test ID or alias.</param>
    /// <param name="requestModel">Optional overrides for profile and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test execution metrics.</returns>
    [HttpPost("{idOrAlias}/run")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestMetricsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestMetricsResponseModel>> RunTest(
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
            return NotFound(CreateProblemDetails("Test not found", "The requested test could not be found."));
        }

        // Execute the test and get metrics
        var metrics = await _testService.RunTestAsync(
            existing.Id,
            requestModel?.ProfileIdOverride,
            requestModel?.ContextIdsOverride,
            batchId: null,
            cancellationToken);

        var responseModel = _umbracoMapper.Map<TestMetricsResponseModel>(metrics)!;
        return Ok(responseModel);
    }
}
