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
/// Controller to run a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class RunTestController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public RunTestController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Run a test with optional profile and context overrides.
    /// </summary>
    [HttpPost("{idOrAlias}/run")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestMetricsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Run(
        string idOrAlias,
        [FromQuery] Guid? profileIdOverride = null,
        [FromQuery] Guid[]? contextIdsOverride = null,
        CancellationToken cancellationToken = default)
    {
        // Get test
        var test = Guid.TryParse(idOrAlias, out var id)
            ? await _testService.GetTestAsync(id, cancellationToken)
            : await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);

        if (test is null)
        {
            return TestNotFound();
        }

        // Run test with overrides
        var metrics = await _testService.RunTestAsync(
            test.Id,
            profileIdOverride,
            contextIdsOverride,
            null, // batchId
            cancellationToken);

        var response = _mapper.Map<TestMetricsResponseModel>(metrics);
        return Ok(response);
    }
}
