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
/// Controller to get a test run by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdTestRunController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdTestRunController"/> class.
    /// </summary>
    public ByIdTestRunController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets a test run by its unique identifier.
    /// </summary>
    /// <param name="id">The test run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test run.</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestRunResponseModel>> GetTestRunById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var run = await _runService.GetTestRunAsync(id, cancellationToken);

        if (run is null)
        {
            return NotFound(CreateProblemDetails("Test run not found", "The requested test run could not be found."));
        }

        var responseModel = _mapper.Map<TestRunResponseModel>(run)!;
        return Ok(responseModel);
    }
}
