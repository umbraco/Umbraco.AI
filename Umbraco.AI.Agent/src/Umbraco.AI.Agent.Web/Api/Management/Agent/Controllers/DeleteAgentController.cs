using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for deleting Agents.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteAgentController : AgentControllerBase
{
    private readonly IAIAgentService _AIAgentService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public DeleteAgentController(IAIAgentService AIAgentService)
    {
        _AIAgentService = AIAgentService;
    }

    /// <summary>
    /// Deletes a agent.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete($"{{{nameof(agentIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAgent(
        IdOrAlias agentIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agentId = await _AIAgentService.TryGetAgentIdAsync(agentIdOrAlias, cancellationToken);
        if (agentId is null)
        {
            return AgentNotFound();
        }

        var deleted = await _AIAgentService.DeleteAgentAsync(agentId.Value, cancellationToken);
        if (!deleted)
        {
            return AgentNotFound();
        }

        return NoContent();
    }
}
