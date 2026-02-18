using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.TestRun.Controllers;

/// <summary>
/// Controller to delete a test run.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class DeleteTestRunController : TestRunControllerBase
{
    private readonly IAITestRunService _runService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTestRunController"/> class.
    /// </summary>
    public DeleteTestRunController(IAITestRunService runService)
    {
        _runService = runService;
    }

    /// <summary>
    /// Deletes a test run and its associated transcript.
    /// </summary>
    /// <param name="id">The test run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _runService.DeleteRunAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(CreateProblemDetails("Test run not found", "The requested test run could not be found."));
        }

        return NoContent();
    }
}
