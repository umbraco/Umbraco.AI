using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get capabilities for a specific connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CapabilitiesConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilitiesConnectionController"/> class.
    /// </summary>
    public CapabilitiesConnectionController(IAiConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Get capabilities for a specific connection.
    /// </summary>
    /// <remarks>
    /// Returns the capabilities supported by the provider of the specified connection.
    /// </remarks>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of capability names.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}/capabilities")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapabilities(
        IdOrAlias connectionIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var connectionId = await _connectionService.TryGetConnectionIdAsync(connectionIdOrAlias, cancellationToken);
        if (connectionId is null)
        {
            return ConnectionNotFound();
        }

        var configured = await _connectionService.GetConfiguredProviderAsync(connectionId.Value, cancellationToken);
        if (configured is null)
        {
            return ConnectionNotFound();
        }

        var capabilities = configured.GetCapabilities()
            .Select(c => c.Kind.ToString())
            .Distinct();

        return Ok(capabilities);
    }
}
