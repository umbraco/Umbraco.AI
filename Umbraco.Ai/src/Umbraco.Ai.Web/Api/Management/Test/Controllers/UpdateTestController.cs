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
/// Controller to update an existing test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdateTestController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public UpdateTestController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Update an existing test.
    /// </summary>
    [HttpPut("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string idOrAlias, UpdateTestRequestModel model, CancellationToken cancellationToken = default)
    {
        // Get existing test
        var existing = Guid.TryParse(idOrAlias, out var id)
            ? await _testService.GetTestAsync(id, cancellationToken)
            : await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);

        if (existing is null)
        {
            return TestNotFound();
        }

        // Map updates to existing entity (preserves Id and Alias)
        _mapper.Map(model, existing);

        // Save
        var updated = await _testService.SaveTestAsync(existing, cancellationToken);
        var response = _mapper.Map<TestResponseModel>(updated);

        return Ok(response);
    }
}
