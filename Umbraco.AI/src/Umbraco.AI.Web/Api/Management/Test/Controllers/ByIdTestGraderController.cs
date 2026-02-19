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
/// Controller to get a test grader by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdTestGraderController : TestControllerBase
{
    private readonly AITestGraderCollection _testGraders;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdTestGraderController"/> class.
    /// </summary>
    public ByIdTestGraderController(AITestGraderCollection testGraders, IUmbracoMapper umbracoMapper)
    {
        _testGraders = testGraders;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a test grader by its ID including configuration schema.
    /// </summary>
    /// <param name="id">The unique identifier of the test grader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test grader details with configuration schema.</returns>
    [HttpGet("test-graders/{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestGraderResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetTestGraderById(
        string id,
        CancellationToken cancellationToken = default)
    {
        IAITestGrader? grader = _testGraders.FirstOrDefault(g =>
            g.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (grader is null)
        {
            return Task.FromResult(TestNotFound());
        }

        var responseModel = _umbracoMapper.Map<IAITestGrader, TestGraderResponseModel>(grader);
        return Task.FromResult<IActionResult>(Ok(responseModel));
    }
}
