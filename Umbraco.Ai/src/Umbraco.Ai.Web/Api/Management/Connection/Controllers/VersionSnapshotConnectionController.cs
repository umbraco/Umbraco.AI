using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get a specific version snapshot of a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class VersionSnapshotConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionSnapshotConnectionController"/> class.
    /// </summary>
    public VersionSnapshotConnectionController(
        IAiConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a specific version snapshot of a connection.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier (GUID) or alias of the connection.</param>
    /// <param name="snapshotVersion">The version number to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection at the specified version.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}/versions/{{{nameof(snapshotVersion)}:int}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConnectionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConnectionVersionSnapshot(
        [FromRoute] IdOrAlias connectionIdOrAlias,
        [FromRoute] int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(connectionIdOrAlias, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        var snapshot = await _connectionService.GetConnectionVersionSnapshotAsync(connection.Id, snapshotVersion, cancellationToken);
        if (snapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {snapshotVersion} was not found for this connection."));
        }

        return Ok(_umbracoMapper.Map<ConnectionResponseModel>(snapshot));
    }
}
