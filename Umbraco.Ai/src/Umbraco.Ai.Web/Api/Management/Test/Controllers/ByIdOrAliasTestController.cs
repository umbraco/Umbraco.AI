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
/// Controller to get a test by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdOrAliasTestController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public ByIdOrAliasTestController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get a test by its ID or alias.
    /// </summary>
    [HttpGet("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string idOrAlias, CancellationToken cancellationToken = default)
    {
        // Try to parse as Guid first
        var test = Guid.TryParse(idOrAlias, out var id)
            ? await _testService.GetTestAsync(id, cancellationToken)
            : await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);

        if (test is null)
        {
            return TestNotFound();
        }

        return Ok(_mapper.Map<TestResponseModel>(test));
    }
}
