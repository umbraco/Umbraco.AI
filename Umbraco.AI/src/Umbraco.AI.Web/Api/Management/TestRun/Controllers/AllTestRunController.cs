using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Api.Management.TestRun.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Controller to get all test runs with pagination and filtering.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllTestRunController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestRunController"/> class.
    /// </summary>
    public AllTestRunController(IAITestRunService runService, IUmbracoMapper mapper)
    {
        _runService = runService;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets a paged list of test runs with optional filtering.
    /// </summary>
    /// <param name="testId">Optional test ID to filter by.</param>
    /// <param name="batchId">Optional batch ID to filter by.</param>
    /// <param name="status">Optional status to filter by (Running, Passed, Failed, Error).</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of test runs.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedModel<TestRunResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedModel<TestRunResponseModel>>> GetAllTestRuns(
        [FromQuery] Guid? testId,
        [FromQuery] Guid? batchId,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        // Parse status if provided
        AITestRunStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AITestRunStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var pagedRuns = await _runService.GetRunsPagedAsync(testId, batchId, statusFilter, skip, take, cancellationToken);

        var responseItems = pagedRuns.Items.Select(run => _mapper.Map<TestRunResponseModel>(run)!);

        return Ok(new PagedModel<TestRunResponseModel>(pagedRuns.Total, responseItems));
    }
}
