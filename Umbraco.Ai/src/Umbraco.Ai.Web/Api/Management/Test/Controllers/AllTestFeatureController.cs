using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Tests;
using Umbraco.Ai.Web.Api.Management.Test.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get all registered test features.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllTestFeatureController : TestControllerBase
{
    private readonly AiTestFeatureCollection _testFeatures;

    public AllTestFeatureController(AiTestFeatureCollection testFeatures)
    {
        _testFeatures = testFeatures;
    }

    /// <summary>
    /// Get all registered test features.
    /// </summary>
    [HttpGet("test-features")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestFeatureInfoModel>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var features = _testFeatures.Select(f => new TestFeatureInfoModel
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            Category = f.Category,
            HasTestCaseSchema = f.TestCaseType is not null
        });

        return Ok(features);
    }
}
