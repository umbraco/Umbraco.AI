using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to check if a context alias exists.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ContextAliasExistsController : ContextControllerBase
{
    private readonly IAIContextService _contextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextAliasExistsController"/> class.
    /// </summary>
    public ContextAliasExistsController(IAIContextService contextService)
    {
        _contextService = contextService;
    }

    /// <summary>
    /// Check if a context with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional context ID to exclude from the check (for editing existing contexts).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a context with the alias exists, false otherwise.</returns>
    [HttpGet($"{{{nameof(alias)}}}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ContextAliasExists(
        [FromRoute] string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _contextService.ContextAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}
