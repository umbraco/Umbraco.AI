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
/// Controller to create a new test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateTestController : TestControllerBase
{
    private readonly IAiTestService _testService;
    private readonly IUmbracoMapper _mapper;

    public CreateTestController(IAiTestService testService, IUmbracoMapper mapper)
    {
        _testService = testService;
        _mapper = mapper;
    }

    /// <summary>
    /// Create a new test.
    /// </summary>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateTestRequestModel model, CancellationToken cancellationToken = default)
    {
        // Check for duplicate alias
        if (await _testService.TestAliasExistsAsync(model.Alias, null, cancellationToken))
        {
            return DuplicateAliasResult();
        }

        // Map and create
        var test = _mapper.Map<AiTest>(model)!;
        var created = await _testService.SaveTestAsync(test, cancellationToken);

        var response = _mapper.Map<TestResponseModel>(created);
        return CreatedAtAction(nameof(ByIdOrAliasTestController.Get), "ByIdOrAliasTest", new { idOrAlias = created.Id }, response);
    }
}
