using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Controller for retrieving all orchestrations.
/// </summary>
[ApiVersion("1.0")]
public class AllOrchestrationController : OrchestrationControllerBase
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllOrchestrationController(IAIOrchestrationService orchestrationService, IUmbracoMapper umbracoMapper)
    {
        _orchestrationService = orchestrationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all orchestrations with optional paging and filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter for name/alias.</param>
    /// <param name="surfaceId">Optional surface ID filter.</param>
    /// <param name="isActive">Optional active status filter. If true, returns only active orchestrations; if false, returns only inactive; if null, returns all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of orchestrations.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<OrchestrationItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrchestrations(
        int skip = 0,
        int take = 100,
        string? filter = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orchestrationService.GetOrchestrationsPagedAsync(skip, take, filter, surfaceId, isActive, cancellationToken);

        var viewModel = new PagedViewModel<OrchestrationItemResponseModel>
        {
            Total = result.Total,
            Items = _umbracoMapper.MapEnumerable<AIOrchestration, OrchestrationItemResponseModel>(result.Items)
        };

        return Ok(viewModel);
    }
}
