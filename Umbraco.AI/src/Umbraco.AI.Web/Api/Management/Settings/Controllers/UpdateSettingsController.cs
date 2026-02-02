using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Settings.Controllers;

/// <summary>
/// Controller to update the AI settings.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdateSettingsController : SettingsControllerBase
{
    private readonly IAISettingsService _settingsService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsController"/> class.
    /// </summary>
    public UpdateSettingsController(IAISettingsService settingsService, IUmbracoMapper umbracoMapper)
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
        var settings = _umbracoMapper.Map<AISettings>(requestModel)!;
        var saved = await _settingsService.SaveSettingsAsync(settings, cancellationToken);
        return Ok(_umbracoMapper.Map<SettingsResponseModel>(saved));
    }
}
