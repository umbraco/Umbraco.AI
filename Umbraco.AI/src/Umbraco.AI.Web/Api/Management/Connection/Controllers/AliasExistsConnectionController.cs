using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Connections;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to check if a connection alias exists.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AliasExistsConnectionController : ConnectionControllerBase
{
    private readonly IAIConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasExistsConnectionController"/> class.
    /// </summary>
    public AliasExistsConnectionController(IAIConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Checks if a connection with the given alias exists.
    /// </summary>
    /// <param name="alias">The alias to check.</param>
    /// <param name="excludeId">Optional connection ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the alias exists, false otherwise.</returns>
    [HttpGet("{alias}/exists")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConnectionAliasExists(
        string alias,
        [FromQuery] Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _connectionService.ConnectionAliasExistsAsync(alias, excludeId, cancellationToken);
        return Ok(exists);
    }
}
