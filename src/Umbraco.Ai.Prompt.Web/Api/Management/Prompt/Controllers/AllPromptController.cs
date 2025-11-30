using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for retrieving all prompts.
/// </summary>
[ApiVersion("1.0")]
public class AllPromptController : PromptControllerBase
{
    private readonly IPromptService _promptService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllPromptController(IPromptService promptService, IUmbracoMapper mapper)
    {
        _promptService = promptService;
        _mapper = mapper;
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
    [ProducesResponseType(typeof(PagedModel<PromptItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        int skip = 0,
        int take = 100,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _promptService.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

        var items = result.Items.Select(p => new PromptItemResponseModel
        {
            Id = p.Id,
            Alias = p.Alias,
            Name = p.Name,
            Description = p.Description,
            ProfileId = p.ProfileId,
            IsActive = p.IsActive
        }).ToList();

        return Ok(new PagedModel<PromptItemResponseModel>(result.Total, items));
    }
}
