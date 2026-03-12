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
/// Controller to get a test run's transcript.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class TranscriptTestRunController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptTestRunController"/> class.
    /// </summary>
    public TranscriptTestRunController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets the transcript for a test run.
    /// </summary>
    /// <param name="id">The test run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test run transcript.</returns>
    [HttpGet("{id:guid}/transcript")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestTranscriptResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestTranscriptResponseModel>> GetTestRunTranscript(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (run, transcript) = await _runService.GetTestRunWithTranscriptAsync(id, cancellationToken);

        if (run is null)
        {
            return NotFound(CreateProblemDetails("Test run not found", "The requested test run could not be found."));
        }

        if (transcript is null)
        {
            return NotFound(CreateProblemDetails("Transcript not found", "The requested test run does not have a transcript."));
        }

        var responseModel = _mapper.Map<TestTranscriptResponseModel>(transcript)!;
        return Ok(responseModel);
    }
}
