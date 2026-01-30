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
/// Controller to get test runs with paging.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllTestRunController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public AllTestRunController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all runs for a specific test with paging.
    /// </summary>
    [HttpGet("runs")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedTestRunResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid testId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        // Verify test exists
        var test = await _testService.GetTestAsync(testId, cancellationToken);
        if (test is null)
        {
            return TestNotFound();
        }

        var (items, total) = await _testService.GetRunsPagedAsync(testId, skip, take, cancellationToken);

        var responseItems = _mapper.MapEnumerable<AiTestRun, TestRunResponseModel>(items);

        return Ok(new PagedTestRunResponseModel
        {
            Items = responseItems.ToList(),
            Total = total
        });
    }
}

/// <summary>
/// Paged test run response model.
/// </summary>
public class PagedTestRunResponseModel
{
    /// <summary>
    /// The test runs in this page.
    /// </summary>
    public required IReadOnlyList<TestRunResponseModel> Items { get; init; }

    /// <summary>
    /// The total number of runs.
    /// </summary>
    public int Total { get; init; }
}
