using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller to check if an agent alias exists.
/// </summary>
[ApiVersion("1.0")]
public class AliasExistsAgentController : AgentControllerBase
{
    private readonly IAIAgentService _aiAgentService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AliasExistsAgentController(IAIAgentService aiAgentService)
    {
        _aiAgentService = aiAgentService;
    }

    /// <summary>
    /// Checks if an agent with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional agent ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the alias exists, false otherwise.</returns>
    [HttpGet("{alias}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> AgentAliasExists(
        string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _aiAgentService.AgentAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}
