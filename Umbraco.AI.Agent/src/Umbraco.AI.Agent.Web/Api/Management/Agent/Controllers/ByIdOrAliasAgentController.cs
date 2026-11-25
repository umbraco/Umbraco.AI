using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving a agent by ID or alias.
/// </summary>
[ApiVersion("1.0")]
public class ByIdOrAliasAgentController : AgentControllerBase
{
    private readonly IAIAgentService _AIAgentService;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IAIAgentUserGroupResolutionService _userGroupResolutionService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIAgentUserGroupResolutionService so the agent can be requested on behalf of another user. Will be removed in v20.")]
    public ByIdOrAliasAgentController(IAIAgentService AIAgentService, IUmbracoMapper umbracoMapper)
        : this(AIAgentService, umbracoMapper, StaticServiceProvider.Instance.GetRequiredService<IAIAgentUserGroupResolutionService>())
    {
    }

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor so <c>ActivatorUtilities</c> does not find two applicable
    /// constructors while the obsolete overload is kept for compatibility.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public ByIdOrAliasAgentController(
        IAIAgentService AIAgentService,
        IUmbracoMapper umbracoMapper,
        IAIAgentUserGroupResolutionService userGroupResolutionService)
    {
        _AIAgentService = AIAgentService;
        _umbracoMapper = umbracoMapper;
        _userGroupResolutionService = userGroupResolutionService;
    }

    /// <summary>
    /// Gets a agent by its ID or alias.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias (string).</param>
    /// <param name="onBehalfOf">
    /// Get the agent as another user sees it instead of the caller, as <c>kind:value</c> (for example
    /// <c>automate-workspace:&lt;workspaceId&gt;</c>). The resolver for the kind checks the caller may act for it.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found and visible to the caller.</returns>
    [HttpGet($"{{{nameof(agentIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AgentResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgentByIdOrAlias(
        IdOrAlias agentIdOrAlias,
        string? onBehalfOf = null,
        CancellationToken cancellationToken = default)
    {
        Core.Agents.AIAgent? agent;
        if (onBehalfOf is null)
        {
            // Managed access: a user with Manage agents sees an agent they can't use, to edit it.
            agent = await _AIAgentService.GetAgentAsync(agentIdOrAlias, includeRestricted: true, cancellationToken);
        }
        else
        {
            var groups = await _userGroupResolutionService.ResolveUserGroupsAsync(onBehalfOf, cancellationToken);
            if (!groups.Success)
            {
                return OnBehalfOfFailed(groups.Status);
            }

            agent = await _AIAgentService.GetAgentAsync(agentIdOrAlias, groups.Result!, cancellationToken);
        }

        if (agent is null)
        {
            return AgentNotFound();
        }

        return Ok(_umbracoMapper.Map<AgentResponseModel>(agent));
    }

    /// <summary>
    /// Gets an agent by ID or alias.
    /// </summary>
    [NonAction]
    [Obsolete("Use the overload that accepts onBehalfOf. Will be removed in v20.")]
    public Task<IActionResult> GetAgentByIdOrAlias(IdOrAlias agentIdOrAlias, CancellationToken cancellationToken)
        => GetAgentByIdOrAlias(agentIdOrAlias, null, cancellationToken);
}
