using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving a agent by ID or alias.
/// </summary>
[ApiVersion("1.0")]
public class ByIdOrAliasAgentController : AgentControllerBase
{
    private readonly IAIAgentService _AiAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public ByIdOrAliasAgentController(IAIAgentService AIAgentService, IUmbracoMapper umbracoMapper)
    {
        _AiAgentService = AIAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets a agent by its ID or alias.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found.</returns>
    [HttpGet($"{{{nameof(agentIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AgentResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgentByIdOrAlias(
        IdOrAlias agentIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agent = await _AiAgentService.GetAgentAsync(agentIdOrAlias, cancellationToken);
        if (agent is null)
        {
            return AgentNotFound();
        }

        return Ok(_umbracoMapper.Map<AgentResponseModel>(agent));
    }
}
