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
/// Controller to get a test run by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdTestRunController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public ByIdTestRunController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get a test run by its ID with optional transcript.
    /// </summary>
    [HttpGet("runs/{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] bool includeTranscript = false,
        CancellationToken cancellationToken = default)
    {
        var run = includeTranscript
            ? await _testService.GetRunWithTranscriptAsync(id, cancellationToken)
            : await _testService.GetRunAsync(id, cancellationToken);

        if (run is null)
        {
            return TestRunNotFound();
        }

        return Ok(_mapper.Map<TestRunResponseModel>(run));
    }
}
