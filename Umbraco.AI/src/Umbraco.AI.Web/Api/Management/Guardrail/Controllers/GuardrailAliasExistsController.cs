using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Controller to check if a guardrail alias exists.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class GuardrailAliasExistsController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuardrailAliasExistsController"/> class.
    /// </summary>
    public GuardrailAliasExistsController(IAIGuardrailService guardrailService)
    {
        _guardrailService = guardrailService;
    }

    /// <summary>
    /// Check if a guardrail with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional guardrail ID to exclude from the check (for editing existing guardrails).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a guardrail with the alias exists, false otherwise.</returns>
    [HttpGet($"{{{nameof(alias)}}}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> GuardrailAliasExists(
        [FromRoute] string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _guardrailService.GuardrailAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}
