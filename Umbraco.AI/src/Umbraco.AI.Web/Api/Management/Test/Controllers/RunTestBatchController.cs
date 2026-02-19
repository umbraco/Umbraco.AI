using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to execute multiple tests in batch.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class RunTestBatchController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunTestBatchController"/> class.
    /// </summary>
    public RunTestBatchController(IAITestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Executes multiple tests in batch and returns metrics for each.
    /// All tests in the batch share the same batch ID for grouping.
    /// </summary>
    /// <param name="requestModel">The batch execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch execution results with metrics for each test.</returns>
    [HttpPost("run-batch")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestBatchResultsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestBatchResultsResponseModel>> RunTestBatch(
        [FromBody] RunTestBatchRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        if (requestModel.TestIds == null || !requestModel.TestIds.Any())
        {
            return BadRequest(CreateProblemDetails(
                "Invalid request",
                "At least one test ID must be provided."));
        }

        var results = await _testService.RunTestBatchAsync(
            requestModel.TestIds,
            requestModel.ProfileIdOverride,
            requestModel.ContextIdsOverride,
            cancellationToken);

        var responseModel = new TestBatchResultsResponseModel
        {
            Results = results.ToDictionary(
                kvp => kvp.Key,
                kvp => _mapper.Map<TestMetricsResponseModel>(kvp.Value)!)
        };

        return Ok(responseModel);
    }
}
