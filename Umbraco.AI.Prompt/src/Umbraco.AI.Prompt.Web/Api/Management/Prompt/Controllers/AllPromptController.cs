using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for retrieving all prompts.
/// </summary>
[ApiVersion("1.0")]
public class AllPromptController : PromptControllerBase
{
    private readonly IAIPromptService _aiPromptService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllPromptController(IAIPromptService aiPromptService, IUmbracoMapper umbracoMapper)
    {
        _aiPromptService = aiPromptService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all prompts with optional paging and filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of prompts.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<PromptItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPrompts(
        int skip = 0,
        int take = 100,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiPromptService.GetPromptsPagedAsync(skip, take, filter, profileId, cancellationToken);

        var viewModel = new PagedViewModel<PromptItemResponseModel>
        {
            Total = result.Total,
            Items = _umbracoMapper.MapEnumerable<Core.Prompts.AIPrompt, PromptItemResponseModel>(result.Items)
        };

        return Ok(viewModel);
    }
}
