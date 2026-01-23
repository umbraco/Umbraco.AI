using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Settings;
using Umbraco.Ai.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Settings.Controllers;

/// <summary>
/// Controller to update the AI settings.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdateSettingsController : SettingsControllerBase
{
    private readonly IAiSettingsService _settingsService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsController"/> class.
    /// </summary>
    public UpdateSettingsController(IAiSettingsService settingsService, IUmbracoMapper umbracoMapper)
    {
        _settingsService = settingsService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update the AI settings.
    /// </summary>
    /// <param name="requestModel">The updated settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    [HttpPut]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SettingsResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings(
        UpdateSettingsRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var settings = _umbracoMapper.Map<AiSettings>(requestModel)!;
        var saved = await _settingsService.SaveSettingsAsync(settings, cancellationToken);
        return Ok(_umbracoMapper.Map<SettingsResponseModel>(saved));
    }
}
