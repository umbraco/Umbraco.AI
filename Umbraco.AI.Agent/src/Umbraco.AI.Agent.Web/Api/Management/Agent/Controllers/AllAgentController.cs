using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
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

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllAgentController(IAIAgentService AIAgentService, IUmbracoMapper umbracoMapper)
    {
        _AIAgentService = AIAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all Agents with optional paging and filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="scopeId">Optional scope ID filter. Only returns agents with this scope assigned.</param>
    /// <param name="isActive">Optional active status filter. If true, returns only active agents; if false, returns only inactive agents; if null, returns all agents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of Agents.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<AgentItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAgents(
        int skip = 0,
        int take = 100,
        string? filter = null,
        Guid? profileId = null,
        string? scopeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _AIAgentService.GetAgentsPagedAsync(skip, take, filter, profileId, scopeId, isActive, cancellationToken);

        var viewModel = new PagedViewModel<AgentItemResponseModel>
        {
            Total = result.Total,
            Items = _umbracoMapper.MapEnumerable<Core.Agents.AIAgent, AgentItemResponseModel>(result.Items)
        };

        return Ok(viewModel);
    }
}
