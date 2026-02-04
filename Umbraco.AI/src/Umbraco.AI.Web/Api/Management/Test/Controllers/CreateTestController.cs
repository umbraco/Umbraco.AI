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
/// Controller to create a new test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateTestController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly AITestFeatureCollection _testFeatures;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTestController"/> class.
    /// </summary>
    public CreateTestController(
        IAITestService testService,
        AITestFeatureCollection testFeatures,
        IUmbracoMapper umbracoMapper)
    {
        _testService = testService;
        _testFeatures = testFeatures;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Create a new test.
    /// </summary>
    /// <param name="requestModel">The test to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created test ID.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTest(
        CreateTestRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate alias
        var existingByAlias = await _testService.GetTestByAliasAsync(requestModel.Alias, cancellationToken);
        if (existingByAlias is not null)
        {
            return TestOperationStatusResult(TestOperationStatus.DuplicateAlias);
        }

        // Validate test type exists
        var testFeature = _testFeatures.FirstOrDefault(f => f.Id == requestModel.TestTypeId);
        if (testFeature is null)
        {
            return TestOperationStatusResult(TestOperationStatus.InvalidTestType);
        }

        AITest test = _umbracoMapper.Map<AITest>(requestModel)!;
        var created = await _testService.SaveTestAsync(test, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdOrAliasTestController.GetTestByIdOrAlias),
            nameof(ByIdOrAliasTestController).Replace("Controller", string.Empty),
            new { idOrAlias = created.Id },
            created.Id.ToString());
    }
}
