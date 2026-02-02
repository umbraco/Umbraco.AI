using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Controller to get all profiles.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllProfileController : ProfileControllerBase
{
    private readonly IAiProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllProfileController"/> class.
    /// </summary>
    public AllProfileController(IAiProfileService profileService, IUmbracoMapper umbracoMapper)
    {
        _profileService = profileService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all profiles.
    /// </summary>
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="capability">Optional capability filter (Chat, Embedding, etc.).</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of profiles.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<ProfileItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<ProfileItemResponseModel>>> GetAllProfiles(
        string? filter = null,
        string? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        AICapability? capabilityFilter = null;
        if (!string.IsNullOrEmpty(capability) && Enum.TryParse<AICapability>(capability, true, out var cap))
        {
            capabilityFilter = cap;
        }

        var (profiles, total) = await _profileService.GetProfilesPagedAsync(
            filter,
            capabilityFilter,
            skip,
            take,
            cancellationToken);

        var viewModel = new PagedViewModel<ProfileItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AIProfile, ProfileItemResponseModel>(profiles)
        };

        return Ok(viewModel);
    }
}
