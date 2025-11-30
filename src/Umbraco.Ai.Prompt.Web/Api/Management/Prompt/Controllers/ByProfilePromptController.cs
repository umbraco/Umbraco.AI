using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for retrieving prompts by profile ID.
/// </summary>
[ApiVersion("1.0")]
public class ByProfilePromptController : PromptControllerBase
{
    private readonly IPromptService _promptService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public ByProfilePromptController(IPromptService promptService)
    {
        _promptService = promptService;
    }

    /// <summary>
    /// Gets all prompts linked to a specific profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of prompts linked to the profile.</returns>
    [HttpGet("profile/{profileId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PromptItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProfile(Guid profileId, CancellationToken cancellationToken = default)
    {
        var prompts = await _promptService.GetByProfileAsync(profileId, cancellationToken);

        var items = prompts.Select(p => new PromptItemResponseModel
        {
            Id = p.Id,
            Alias = p.Alias,
            Name = p.Name,
            Description = p.Description,
            ProfileId = p.ProfileId,
            IsActive = p.IsActive
        });

        return Ok(items);
    }
}
