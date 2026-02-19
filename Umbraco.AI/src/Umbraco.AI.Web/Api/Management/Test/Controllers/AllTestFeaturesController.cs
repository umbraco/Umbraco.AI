using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to list all available test features (test types).
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
[UmbracoAIVersionedManagementApiRoute("test-features")]
public class AllTestFeaturesController : TestControllerBase
{
    private readonly AITestFeatureCollection _testFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestFeaturesController"/> class.
    /// </summary>
    public AllTestFeaturesController(AITestFeatureCollection testFeatures)
    {
        _testFeatures = testFeatures;
    }

    /// <summary>
    /// Gets all available test features (test types) that can be used to create tests.
    /// Test features are discovered via the [AITestFeature] attribute and registered in DI.
    /// </summary>
    /// <returns>List of available test features.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestFeatureInfoModel>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TestFeatureInfoModel>> GetAllTestFeatures()
    {
        var features = _testFeatures.Select(feature => new TestFeatureInfoModel
        {
            Id = feature.Id,
            Name = feature.Name,
            Description = feature.Description,
            Category = feature.Category
        });

        return Ok(features);
    }
}
