using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for running agents with non-streaming JSON responses.
/// </summary>
[ApiVersion("1.0")]
public class RunAgentController : AgentControllerBase
{
    private readonly IAIAgentService _agentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentController"/> class.
    /// </summary>
    public RunAgentController(
        IAIAgentService agentService,
        IUmbracoMapper umbracoMapper)
    {
        _agentService = agentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Runs an agent and returns the complete response as JSON.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="requestModel">The run request containing messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent response.</returns>
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/run")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunAgent(
        IdOrAlias agentIdOrAlias,
        RunAgentRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var agentId = await _agentService.TryGetAgentIdAsync(agentIdOrAlias, cancellationToken);
        if (agentId is null)
        {
            return AgentNotFound();
        }

        try
        {
            var messages = _umbracoMapper.MapEnumerable<ChatMessageModel, ChatMessage>(requestModel.Messages).ToList();
            var response = await _agentService.RunAgentAsync(agentId.Value, messages, cancellationToken: cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Agent execution failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
