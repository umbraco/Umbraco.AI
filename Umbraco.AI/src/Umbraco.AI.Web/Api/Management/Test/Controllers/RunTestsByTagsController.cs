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
/// Controller to execute tests filtered by tags.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class RunTestsByTagsController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunTestsByTagsController"/> class.
    /// </summary>
    public RunTestsByTagsController(IAITestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Executes all tests with the specified tags and returns metrics for each.
    /// Tests must have ALL specified tags to be included.
    /// All tests share the same batch ID for grouping.
    /// </summary>
    /// <param name="requestModel">The tags execution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch execution results with metrics for each matching test.</returns>
    [HttpPost("~/umbraco/ai/management/api/v{version:apiVersion}/tests/execute-by-tags")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestBatchResultsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestBatchResultsResponseModel>> RunByTags(
        [FromBody] RunTestsByTagsRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        if (requestModel.Tags == null || !requestModel.Tags.Any())
        {
            return BadRequest(CreateProblemDetails(
                "Invalid request",
                "At least one tag must be provided."));
        }

        var results = await _testService.RunTestsByTagsAsync(
            requestModel.Tags,
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
