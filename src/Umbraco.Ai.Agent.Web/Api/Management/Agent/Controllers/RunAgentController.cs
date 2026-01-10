using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Agui;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for running agents with AG-UI streaming support.
/// </summary>
/// <remarks>
/// <para>
/// This controller uses the Microsoft Agent Framework (MAF) for agent execution,
/// but maintains a custom implementation rather than using MAF's built-in <c>MapAGUI()</c>
/// for the following reasons:
/// </para>
/// <list type="bullet">
///   <item>Frontend tool handling with <c>FunctionInvokingChatClient.CurrentContext.Terminate</c></item>
///   <item>Umbraco authorization/security model integration</item>
///   <item>Custom AG-UI context item handling</item>
/// </list>
/// </remarks>
[ApiVersion("1.0")]
public class RunAgentController : AgentControllerBase
{
    private readonly IAiAgentService _agentService;
    private readonly IAiProfileService _profileService;
    private readonly IAiAgentFactory _agentFactory;
    private readonly IAguiStreamingService _streamingService;
    private readonly IAguiToolConverter _toolConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentController"/> class.
    /// </summary>
    public RunAgentController(
        IAiAgentService agentService,
        IAiProfileService profileService,
        IAiAgentFactory agentFactory,
        IAguiStreamingService streamingService,
        IAguiToolConverter toolConverter)
    {
        _agentService = agentService;
        _profileService = profileService;
        _agentFactory = agentFactory;
        _streamingService = streamingService;
        _toolConverter = toolConverter;
    }

    /// <summary>
    /// Runs an agent with AG-UI streaming response (SSE).
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="request">The AG-UI run request containing messages and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of AG-UI events.</returns>
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/run")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> RunAgent(
        IdOrAlias agentIdOrAlias,
        AguiRunRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve and validate the agent
        var agent = await _agentService.GetAgentAsync(agentIdOrAlias, cancellationToken);
        if (agent is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "AiAgent not found",
                Detail = "The specified agent could not be found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!agent.IsActive)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Agent not active",
                Detail = $"Agent '{agent.Name}' is not active.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 2. Validate the agent's profile exists
        var profile = await _profileService.GetProfileAsync(agent.ProfileId, cancellationToken);
        if (profile is null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Profile not found",
                Detail = $"The profile configured for agent '{agent.Name}' could not be found.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 3. Convert frontend tools and create MAF agent
        var frontendTools = _toolConverter.ConvertToFrontendTools(request.Tools);
        var mafAgent = await _agentFactory.CreateAgentAsync(agent, frontendTools, cancellationToken);

        // 4. Stream via service
        var events = _streamingService.StreamAgentAsync(
            mafAgent,
            request,
            frontendTools,
            cancellationToken);

        return new AguiEventStreamResult(events);
    }
}
