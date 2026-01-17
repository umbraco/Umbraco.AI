using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for creating Agents.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateAgentController : AgentControllerBase
{
    private readonly IAiAgentService _AiAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public CreateAgentController(IAiAgentService AiAgentService, IUmbracoMapper umbracoMapper)
    {
        _AiAgentService = AiAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Creates a new agent.
    /// </summary>
    /// <param name="model">The agent creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created agent.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AgentResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAgent(
        [FromBody] CreateAgentRequestModel model,
        CancellationToken cancellationToken = default)
    {
        AiAgent agent = _umbracoMapper.Map<AiAgent>(model)!;

        try
        {
            AiAgent created = await _AiAgentService.SaveAgentAsync(agent, cancellationToken);

            return CreatedAtAction(
                nameof(ByIdOrAliasAgentController.GetAgentByIdOrAlias),
                "ByIdOrAliasAgent",
                new { agentIdOrAlias = created.Id },
                created.Id.ToString());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return AliasAlreadyExists(model.Alias);
        }
    }
}
