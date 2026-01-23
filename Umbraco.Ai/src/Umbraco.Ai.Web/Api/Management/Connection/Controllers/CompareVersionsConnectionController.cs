using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to compare two versions of a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CompareVersionsConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareVersionsConnectionController"/> class.
    /// </summary>
    public CompareVersionsConnectionController(IAiConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Compare two versions of a connection.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier (GUID) or alias of the connection.</param>
    /// <param name="snapshotFromVersion">The source version to compare from.</param>
    /// <param name="snapshotToVersion">The target version to compare to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The differences between the two versions.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}/versions/{{{nameof(snapshotFromVersion)}}}/compare/{{{nameof(snapshotToVersion)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(VersionComparisonResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareConnectionVersions(
        [FromRoute] IdOrAlias connectionIdOrAlias,
        [FromRoute] int snapshotFromVersion,
        [FromRoute] int snapshotToVersion,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(connectionIdOrAlias, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        // Get the "from" version - this could be a historical snapshot or the current version
        var fromSnapshot = snapshotFromVersion == connection.Version
            ? connection
            : await _connectionService.GetConnectionVersionSnapshotAsync(connection.Id, snapshotFromVersion, cancellationToken);

        if (fromSnapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {snapshotFromVersion} was not found for this connection."));
        }

        // Get the "to" version - this could be a historical snapshot or the current version
        var toSnapshot = snapshotToVersion == connection.Version
            ? connection
            : await _connectionService.GetConnectionVersionSnapshotAsync(connection.Id, snapshotToVersion, cancellationToken);

        if (toSnapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {snapshotToVersion} was not found for this connection."));
        }

        var changes = CompareConnections(fromSnapshot, toSnapshot);

        return Ok(new VersionComparisonResponseModel
        {
            FromVersion = snapshotFromVersion,
            ToVersion = snapshotToVersion,
            Changes = changes
        });
    }

    private static List<PropertyChangeModel> CompareConnections(AiConnection from, AiConnection to)
    {
        var changes = new List<PropertyChangeModel>();

        // Compare Name
        if (from.Name != to.Name)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Name",
                OldValue = from.Name,
                NewValue = to.Name
            });
        }

        // Compare Alias
        if (from.Alias != to.Alias)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Alias",
                OldValue = from.Alias,
                NewValue = to.Alias
            });
        }

        // Compare ProviderId
        if (from.ProviderId != to.ProviderId)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "ProviderId",
                OldValue = from.ProviderId,
                NewValue = to.ProviderId
            });
        }

        // Compare IsActive
        if (from.IsActive != to.IsActive)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "IsActive",
                OldValue = from.IsActive.ToString(),
                NewValue = to.IsActive.ToString()
            });
        }

        // Compare Settings (as JSON)
        var fromSettings = from.Settings is not null ? JsonSerializer.Serialize(from.Settings) : "";
        var toSettings = to.Settings is not null ? JsonSerializer.Serialize(to.Settings) : "";
        if (fromSettings != toSettings)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Settings",
                OldValue = fromSettings,
                NewValue = toSettings
            });
        }

        return changes;
    }
}
