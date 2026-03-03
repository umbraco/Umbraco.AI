using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Controller to retrieve execution summary with per-variation metrics.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdTestExecutionController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdTestExecutionController"/> class.
    /// </summary>
    public ByIdTestExecutionController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the execution summary for a given execution ID.
    /// Reconstructs per-variation metrics from stored runs.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result with per-variation metrics.</returns>
    [HttpGet("executions/{executionId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestExecutionResultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestExecutionResultResponseModel>> GetExecutionResult(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _runService.GetExecutionResultAsync(executionId, cancellationToken);
        if (result is null)
        {
            return NotFound(CreateProblemDetails("Execution not found", $"No runs found for execution {executionId}."));
        }

        var responseModel = _mapper.Map<TestExecutionResultResponseModel>(result)!;
        return Ok(responseModel);
    }
}
