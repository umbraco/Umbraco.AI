using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get all tests.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllTestController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestController"/> class.
    /// </summary>
    public AllTestController(IAITestService testService, IUmbracoMapper umbracoMapper)
    {
        _testService = testService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all tests.
    /// </summary>
    /// <param name="filter">Optional filter to search by name/alias (case-insensitive contains).</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of tests.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<TestItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<TestItemResponseModel>>> GetAllTests(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _testService.GetTestsPagedAsync(
            skip,
            take,
            filter,
            cancellationToken);

        var viewModel = new PagedViewModel<TestItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AITest, TestItemResponseModel>(items)
        };

        return Ok(viewModel);
    }
}
