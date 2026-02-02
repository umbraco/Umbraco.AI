using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get available capabilities from configured connections.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllCapabilitiesConnectionController : ConnectionControllerBase
{
    private readonly IAIConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllCapabilitiesConnectionController"/> class.
    /// </summary>
    public AllCapabilitiesConnectionController(IAIConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Get all capabilities from configured connections.
    /// </summary>
    /// <remarks>
    /// Returns the unique set of capabilities that are supported by at least one configured connection.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of capability names.</returns>
    [HttpGet("capabilities")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetAllCapabilities(CancellationToken cancellationToken = default)
    {
        var capabilities = await _connectionService.GetAvailableCapabilitiesAsync(cancellationToken);
        return Ok(capabilities.Select(c => c.ToString()));
    }
}
