using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving all Agents.
/// </summary>
[ApiVersion("1.0")]
public class AllAgentController : AgentControllerBase
{
    private readonly IAIAgentService _AIAgentService;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IAIAgentUserGroupResolutionService _userGroupResolutionService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIAgentUserGroupResolutionService so the list can be requested on behalf of another user. Will be removed in v20.")]
    public AllAgentController(IAIAgentService AIAgentService, IUmbracoMapper umbracoMapper)
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
    public AllAgentController(
        IAIAgentService AIAgentService,
        IUmbracoMapper umbracoMapper,
        IAIAgentUserGroupResolutionService userGroupResolutionService)
    {
        _AIAgentService = AIAgentService;
        _umbracoMapper = umbracoMapper;
        _userGroupResolutionService = userGroupResolutionService;
    }

    /// <summary>
    /// Gets all Agents with optional paging and filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="surfaceId">Optional surface ID filter. Only returns agents assigned to this surface.</param>
    /// <param name="isActive">Optional active status filter. If true, returns only active agents; if false, returns only inactive agents; if null, returns all agents.</param>
    /// <param name="agentType">Optional agent type filter ("standard" or "orchestrated").</param>
    /// <param name="includeRestricted">
    /// Ask for every agent, including ones limited to user groups the caller is not in. Only honoured when the
    /// caller has the Manage agents right, and ignored with <paramref name="onBehalfOf"/>.
    /// </param>
    /// <param name="onBehalfOf">
    /// List the agents another user can use instead of the caller's, as <c>kind:value</c> (for example
    /// <c>automate-workspace:&lt;workspaceId&gt;</c>). The resolver for the kind checks the caller may act for it.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of Agents.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<AgentItemResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllAgents(
        int skip = 0,
        int take = 100,
        string? filter = null,
        Guid? profileId = null,
        string? surfaceId = null,
        bool? isActive = null,
        string? agentType = null,
        bool includeRestricted = false,
        string? onBehalfOf = null,
        CancellationToken cancellationToken = default)
    {
        AIAgentType? parsedAgentType = agentType?.ToLowerInvariant() switch
        {
            "standard" => AIAgentType.Standard,
            "orchestrated" => AIAgentType.Orchestrated,
            not null => throw new ArgumentException($"Unknown agent type: {agentType}"),
            _ => null
        };

        (IEnumerable<Core.Agents.AIAgent> Items, int Total) result;
        if (onBehalfOf is null)
        {
            result = await _AIAgentService.GetAgentsPagedAsync(
                skip, take, filter, profileId, surfaceId, isActive, parsedAgentType, includeRestricted, cancellationToken);
        }
        else
        {
            var groups = await _userGroupResolutionService.ResolveUserGroupsAsync(onBehalfOf, cancellationToken);
            if (!groups.Success)
            {
                return OnBehalfOfFailed(groups.Status);
            }

            result = await _AIAgentService.GetAgentsPagedAsync(
                skip, take, filter, profileId, surfaceId, isActive, parsedAgentType, groups.Result!, cancellationToken);
        }

        var viewModel = new PagedViewModel<AgentItemResponseModel>
        {
            Total = result.Total,
            Items = _umbracoMapper.MapEnumerable<Core.Agents.AIAgent, AgentItemResponseModel>(result.Items)
        };

        return Ok(viewModel);
    }
}
