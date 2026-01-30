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
/// Controller to run multiple tests in a batch.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class RunTestBatchController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public RunTestBatchController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Run multiple tests in a batch with optional overrides.
    /// </summary>
    [HttpPost("run")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(Dictionary<Guid, TestMetricsResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunBatch(
        RunTestBatchRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var results = await _testService.RunTestBatchAsync(
            model.TestIds,
            model.ProfileIdOverride,
            model.ContextIdsOverride,
            cancellationToken);

        var response = results.ToDictionary(
            kvp => kvp.Key,
            kvp => _mapper.Map<TestMetricsResponseModel>(kvp.Value));

        return Ok(response);
    }
}
