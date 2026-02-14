using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to update a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateTestController : TestControllerBase
{
    private readonly IAITestService _testService;
    private readonly AITestFeatureCollection _testFeatures;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTestController"/> class.
    /// </summary>
    public UpdateTestController(
        IAITestService testService,
        AITestFeatureCollection testFeatures,
        IUmbracoMapper umbracoMapper)
    {
        _testService = testService;
        _testFeatures = testFeatures;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update a test.
    /// </summary>
    /// <param name="idOrAlias">The test ID or alias.</param>
    /// <param name="requestModel">The updated test data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTest(
        string idOrAlias,
        UpdateTestRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        // Find existing test
        AITest? existing = null;
        if (Guid.TryParse(idOrAlias, out var id))
        {
            existing = await _testService.GetTestAsync(id, cancellationToken);
        }
        if (existing is null)
        {
            existing = await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);
        }
        if (existing is null)
        {
            return TestNotFound();
        }

        // Check for duplicate alias (if changing)
        if (requestModel.Alias != existing.Alias)
        {
            var existingByAlias = await _testService.GetTestByAliasAsync(requestModel.Alias, cancellationToken);
            if (existingByAlias is not null && existingByAlias.Id != existing.Id)
            {
                return TestOperationStatusResult(TestOperationStatus.DuplicateAlias);
            }
        }

        // Validate test type exists
        var testFeature = _testFeatures.FirstOrDefault(f => f.Id == requestModel.TestTypeId);
        if (testFeature is null)
        {
            return TestOperationStatusResult(TestOperationStatus.InvalidTestType);
        }

        // Map request to existing entity
        AITest test = _umbracoMapper.Map<AITest>(requestModel)!;
        test.Id = existing.Id;
        test.DateCreated = existing.DateCreated;
        test.Version = existing.Version;

        await _testService.SaveTestAsync(test, cancellationToken);

        return NoContent();
    }
}
