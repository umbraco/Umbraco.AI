using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to delete a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteConnectionController"/> class.
    /// </summary>
    public DeleteConnectionController(IAiConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Delete a connection.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(connectionIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConnection(
        IdOrAlias connectionIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        // Resolve to ID first since DeleteConnectionAsync requires Guid
        var connectionId = await _connectionService.TryGetConnectionIdAsync(connectionIdOrAlias, cancellationToken);
        if (connectionId is null)
        {
            return ConnectionNotFound();
        }

        try
        {
            await _connectionService.DeleteConnectionAsync(connectionId.Value, cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return ConnectionNotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("in use"))
        {
            return ConnectionOperationStatusResult(ConnectionOperationStatus.InUse);
        }
    }
}
