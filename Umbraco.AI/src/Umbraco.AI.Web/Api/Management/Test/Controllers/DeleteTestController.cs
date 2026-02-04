using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to delete a test.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteTestController : TestControllerBase
{
    private readonly IAITestService _testService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTestController"/> class.
    /// </summary>
    public DeleteTestController(IAITestService testService)
    {
        _testService = testService;
    }

    /// <summary>
    /// Delete a test.
    /// </summary>
    /// <param name="idOrAlias">The test ID or alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTest(
        string idOrAlias,
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

        await _testService.DeleteTestAsync(existing.Id, cancellationToken);

        return NoContent();
    }
}
