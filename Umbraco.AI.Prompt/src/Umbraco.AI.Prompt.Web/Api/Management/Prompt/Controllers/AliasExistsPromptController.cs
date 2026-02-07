using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Core.Prompts;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller to check if a prompt alias exists.
/// </summary>
[ApiVersion("1.0")]
public class AliasExistsPromptController : PromptControllerBase
{
    private readonly IAIPromptService _aiPromptService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AliasExistsPromptController(IAIPromptService aiPromptService)
    {
        _aiPromptService = aiPromptService;
    }

    /// <summary>
    /// Checks if a prompt with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional prompt ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the alias exists, false otherwise.</returns>
    [HttpGet("{alias}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> AliasExists(
        string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _aiPromptService.PromptAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}
