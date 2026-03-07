using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Controller for deleting orchestrations.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class DeleteOrchestrationController : OrchestrationControllerBase
{
    private readonly IAIOrchestrationService _orchestrationService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public DeleteOrchestrationController(IAIOrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    /// <summary>
    /// Deletes an orchestration.
    /// </summary>
    /// <param name="orchestrationIdOrAlias">The orchestration ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete($"{{{nameof(orchestrationIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrchestration(
        IdOrAlias orchestrationIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var orchestrationId = await _orchestrationService.TryGetOrchestrationIdAsync(orchestrationIdOrAlias, cancellationToken);
        if (orchestrationId is null)
        {
            return OrchestrationNotFound();
        }

        var deleted = await _orchestrationService.DeleteOrchestrationAsync(orchestrationId.Value, cancellationToken);
        if (!deleted)
        {
            return OrchestrationNotFound();
        }

        return NoContent();
    }
}
