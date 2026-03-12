using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get a test feature by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
[UmbracoAIVersionedManagementApiRoute("test-features")]
public class ByIdTestFeatureController : TestControllerBase
{
    private readonly AITestFeatureCollection _testFeatures;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdTestFeatureController"/> class.
    /// </summary>
    public ByIdTestFeatureController(AITestFeatureCollection testFeatures, IUmbracoMapper umbracoMapper)
    {
        _testFeatures = testFeatures;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a test feature by its ID including test case schema.
    /// </summary>
    /// <param name="id">The unique identifier of the test feature.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test feature details with test case schema.</returns>
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestFeatureResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetTestFeatureById(
        string id,
        CancellationToken cancellationToken = default)
    {
        IAITestFeature? testFeature = _testFeatures.FirstOrDefault(f => f.Id == id);
        if (testFeature is null)
        {
            return Task.FromResult(TestNotFound());
        }

        var responseModel = _umbracoMapper.Map<IAITestFeature, TestFeatureResponseModel>(testFeature);
        return Task.FromResult<IActionResult>(Ok(responseModel));
    }
}
