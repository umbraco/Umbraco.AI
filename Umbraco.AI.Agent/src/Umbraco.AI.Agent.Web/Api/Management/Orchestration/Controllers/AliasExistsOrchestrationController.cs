using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Controller to check if an orchestration alias exists.
/// </summary>
[ApiVersion("1.0")]
public class AliasExistsOrchestrationController : OrchestrationControllerBase
{
    private readonly IAIOrchestrationService _orchestrationService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AliasExistsOrchestrationController(IAIOrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    /// <summary>
    /// Checks if an orchestration with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional orchestration ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the alias exists, false otherwise.</returns>
    [HttpGet("{alias}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> OrchestrationAliasExists(
        string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _orchestrationService.OrchestrationAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}
