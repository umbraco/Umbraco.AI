using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for deleting Agents.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteAgentController : AgentControllerBase
{
    private readonly IAiAgentService _AiAgentService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public DeleteAgentController(IAiAgentService AiAgentService)
    {
        _AiAgentService = AiAgentService;
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
        var agentId = await _AiAgentService.TryGetAgentIdAsync(agentIdOrAlias, cancellationToken);
        if (agentId is null)
        {
            return AgentNotFound();
        }

        var deleted = await _AiAgentService.DeleteAgentAsync(agentId.Value, cancellationToken);
        if (!deleted)
        {
            return AgentNotFound();
        }

        return NoContent();
    }
}
