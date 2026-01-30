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
/// Controller to get all tests with paging and filtering.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllTestController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public AllTestController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all tests with optional filtering and paging.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedTestResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? filter = null,
        [FromQuery] string? testTypeId = null,
        [FromQuery] bool? isEnabled = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _testService.GetTestsPagedAsync(
            filter,
            testTypeId,
            isEnabled,
            skip,
            take,
            cancellationToken);

        var responseItems = _mapper.MapEnumerable<AiTest, TestResponseModel>(items);

        return Ok(new PagedTestResponseModel
        {
            Items = responseItems.ToList(),
            Total = total
        });
    }
}

/// <summary>
/// Paged test response model.
/// </summary>
public class PagedTestResponseModel
{
    /// <summary>
    /// The tests in this page.
    /// </summary>
    public required IReadOnlyList<TestResponseModel> Items { get; init; }

    /// <summary>
    /// The total number of tests matching the filter.
    /// </summary>
    public int Total { get; init; }
}
