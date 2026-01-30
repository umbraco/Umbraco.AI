using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to test a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class TestConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestConnectionController"/> class.
    /// </summary>
    public TestConnectionController(IAiConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Test a connection by verifying credentials with the provider.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result.</returns>
    [HttpPost($"{{{nameof(connectionIdOrAlias)}}}/test")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConnectionTestResultModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestConnection(
        IdOrAlias connectionIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        // Resolve to ID first since TestConnectionAsync requires Guid
        var connectionId = await _connectionService.TryGetConnectionIdAsync(connectionIdOrAlias, cancellationToken);
        if (connectionId is null)
        {
            return ConnectionNotFound();
        }

        try
        {
            var success = await _connectionService.TestConnectionAsync(connectionId.Value, cancellationToken);
            return Ok(new ConnectionTestResultModel
            {
                Success = success,
                ErrorMessage = success ? null : "Connection test failed"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return ConnectionNotFound();
        }
        catch (Exception ex)
        {
            return Ok(new ConnectionTestResultModel
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
