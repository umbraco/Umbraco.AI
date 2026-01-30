using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Tests;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to delete a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteTestController : TestControllerBase
{
    private readonly IAiTestService _testService;

    public DeleteTestController(IAiTestService testService)
    {
        _testService = testService;
    }

    /// <summary>
    /// Delete a test by ID or alias.
    /// </summary>
    [HttpDelete("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idOrAlias, CancellationToken cancellationToken = default)
    {
        // Get test to verify it exists
        var test = Guid.TryParse(idOrAlias, out var id)
            ? await _testService.GetTestAsync(id, cancellationToken)
            : await _testService.GetTestByAliasAsync(idOrAlias, cancellationToken);

        if (test is null)
        {
            return TestNotFound();
        }

        // Delete
        await _testService.DeleteTestAsync(test.Id, cancellationToken);

        return NoContent();
    }
}
