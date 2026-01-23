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
/// Controller to rollback a connection to a previous version.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class RollbackConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackConnectionController"/> class.
    /// </summary>
    public RollbackConnectionController(
        IAiConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Rollback a connection to a previous version.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier (GUID) or alias of the connection.</param>
    /// <param name="snapshotVersion">The version number to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection at the new version (after rollback).</returns>
    [HttpPost($"{{{nameof(connectionIdOrAlias)}}}/versions/{{{nameof(snapshotVersion)}:int}}/rollback")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConnectionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RollbackConnectionToVersion(
        [FromRoute] IdOrAlias connectionIdOrAlias,
        [FromRoute] int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(connectionIdOrAlias, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        try
        {
            var rolledBackConnection = await _connectionService.RollbackConnectionAsync(connection.Id, snapshotVersion, cancellationToken);
            return Ok(_umbracoMapper.Map<ConnectionResponseModel>(rolledBackConnection));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Version"))
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {snapshotVersion} was not found for this connection."));
        }
    }
}
