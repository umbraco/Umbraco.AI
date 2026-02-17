using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Connections;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to delete a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class DeleteConnectionController : ConnectionControllerBase
{
    private readonly IAIConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteConnectionController"/> class.
    /// </summary>
    public DeleteConnectionController(IAIConnectionService connectionService)
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
