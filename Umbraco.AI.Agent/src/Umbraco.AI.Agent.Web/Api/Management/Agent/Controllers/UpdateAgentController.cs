using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for updating Agents.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateAgentController : AgentControllerBase
{
    private readonly IAIAgentService _AIAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public UpdateAgentController(IAIAgentService AIAgentService, IUmbracoMapper umbracoMapper)
    {
        _AIAgentService = AIAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Updates an existing agent.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias (string).</param>
    /// <param name="model">The agent update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated agent.</returns>
    [HttpPut($"{{{nameof(agentIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAgent(
        IdOrAlias agentIdOrAlias,
        [FromBody] UpdateAgentRequestModel model,
        CancellationToken cancellationToken = default)
    {
        AIAgent? existing = await _AIAgentService.GetAgentAsync(agentIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return AgentNotFound();
        }

        AIAgent agent = _umbracoMapper.Map(model, existing);

        await _AIAgentService.SaveAgentAsync(agent, cancellationToken);
        return Ok();
    }
}
