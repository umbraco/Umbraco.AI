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
/// Controller for pairwise variation comparison within an execution.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class CompareVariationsController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareVariationsController"/> class.
    /// </summary>
    public CompareVariationsController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Compares two variation groups within an execution.
    /// Use sourceVariationId = null for the default config.
    /// </summary>
    /// <param name="requestModel">The variation comparison request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Variation comparison with metrics deltas.</returns>
    [HttpPost("compare-variations")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestVariationComparisonResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestVariationComparisonResponseModel>> CompareVariations(
        [FromBody] CompareVariationsRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comparison = await _runService.CompareVariationsAsync(
                requestModel.ExecutionId,
                requestModel.SourceVariationId,
                requestModel.ComparisonVariationId,
                cancellationToken);

            var responseModel = _mapper.Map<TestVariationComparisonResponseModel>(comparison)!;
            return Ok(responseModel);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Bad request", ex.Message));
        }
    }
}
