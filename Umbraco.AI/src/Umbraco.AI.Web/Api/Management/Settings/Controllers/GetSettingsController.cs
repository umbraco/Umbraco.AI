using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Settings.Controllers;

/// <summary>
/// Controller to get the current AI settings.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class GetSettingsController : SettingsControllerBase
{
    private readonly IAISettingsService _settingsService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSettingsController"/> class.
    /// </summary>
    public GetSettingsController(IAISettingsService settingsService, IUmbracoMapper umbracoMapper)
    {
        _settingsService = settingsService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get the current AI settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current AI settings.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SettingsResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(_umbracoMapper.Map<SettingsResponseModel>(settings));
    }
}
