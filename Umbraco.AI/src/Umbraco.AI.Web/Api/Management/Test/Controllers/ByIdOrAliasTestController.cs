using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get a test by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdOrAliasTestController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdOrAliasTestController"/> class.
    /// </summary>
    public ByIdOrAliasTestController(IAITestService testService, IUmbracoMapper umbracoMapper)
    {
        _testService = testService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a test by ID or alias.
    /// </summary>
    /// <param name="idOrAlias">The test ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test if found.</returns>
    [HttpGet("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestResponseModel>> GetTestByIdOrAlias(
        string idOrAlias,
        CancellationToken cancellationToken = default)
    {
        AITest? test = null;

        // Try to parse as GUID first
        if (Guid.TryParse(idOrAlias, out var id))
        {
            test = await _testService.GetTestAsync(id, cancellationToken);
        }

        // If not found by ID, try by alias
        if (test is null)
        {
            test = await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);
        }

        if (test is null)
        {
            return TestNotFound();
        }

        var responseModel = _umbracoMapper.Map<TestResponseModel>(test)!;
        return Ok(responseModel);
    }
}
