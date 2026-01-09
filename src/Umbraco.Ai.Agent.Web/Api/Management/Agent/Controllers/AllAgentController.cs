using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving all Agents.
/// </summary>
[ApiVersion("1.0")]
public class AllAgentController : AgentControllerBase
{
    private readonly IAiAgentService _AiAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllAgentController(IAiAgentService AiAgentService, IUmbracoMapper umbracoMapper)
    {
        _AiAgentService = AiAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all Agents with optional paging and filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
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
        CancellationToken cancellationToken = default)
    {
        var result = await _AiAgentService.GetAgentsPagedAsync(skip, take, filter, profileId, cancellationToken);

        var viewModel = new PagedViewModel<AgentItemResponseModel>
        {
            Total = result.Total,
            Items = _umbracoMapper.MapEnumerable<Core.Agents.AiAgent, AgentItemResponseModel>(result.Items)
        };

        return Ok(viewModel);
    }
}
