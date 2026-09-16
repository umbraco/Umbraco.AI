using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for suggesting starter prompts from an agent's instructions.
/// </summary>
[ApiVersion("1.0")]
public class SuggestStartersAgentController : AgentControllerBase
{
    private readonly IAIAgentService _agentService;
    private readonly IAIStarterPromptSuggester _suggester;
    private readonly IAIProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestStartersAgentController"/> class.
    /// </summary>
    public SuggestStartersAgentController(
        IAIAgentService agentService,
        IAIStarterPromptSuggester suggester,
        IAIProfileService profileService)
    {
        _agentService = agentService;
        _suggester = suggester;
        _profileService = profileService;
    }

    /// <summary>
    /// Checks whether a profile can be resolved for suggesting starters — either the agent's own
    /// profile, or the default chat profile. Lets the editor disable the button up front instead of
    /// letting the click fail with a 400.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when a profile can be resolved for this agent.</returns>
    [HttpGet($"{{{nameof(agentIdOrAlias)}}}/suggest-starters/availability")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuggestStartersAvailability(
        IdOrAlias agentIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agent = await _agentService.GetAgentAsync(agentIdOrAlias, cancellationToken);
        if (agent is null)
        {
            return AgentNotFound();
        }

        var available = agent.ProfileId.HasValue
            || await _profileService.HasDefaultProfileAsync(AICapability.Chat, cancellationToken);

        return Ok(available);
    }

    /// <summary>
    /// Suggests starter prompts drafted from the agent's instructions. Never persisted — the caller
    /// decides whether to keep, edit or discard the suggestions.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggested starter prompts.</returns>
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/suggest-starters")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SuggestStartersResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuggestStarters(
        IdOrAlias agentIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agent = await _agentService.GetAgentAsync(agentIdOrAlias, cancellationToken);
        if (agent is null)
        {
            return AgentNotFound();
        }

        try
        {
            var starters = await _suggester.SuggestStartersAsync(agent, cancellationToken);
            return Ok(new SuggestStartersResponseModel { Starters = starters });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Suggesting starter prompts failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
