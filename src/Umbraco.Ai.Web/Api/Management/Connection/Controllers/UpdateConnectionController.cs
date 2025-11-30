using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to update an existing connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdateConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateConnectionController"/> class.
    /// </summary>
    public UpdateConnectionController(IAiConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    /// <summary>
    /// Update an existing connection.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection to update.</param>
    /// <param name="requestModel">The updated connection data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut($"{{{nameof(connectionIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConnection(
        IdOrAlias connectionIdOrAlias,
        UpdateConnectionRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var existing = await _connectionService.GetConnectionAsync(connectionIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return ConnectionNotFound();
        }

        var connection = new AiConnection
        {
            Id = existing.Id,
            Alias = requestModel.Alias,
            Name = requestModel.Name,
            ProviderId = existing.ProviderId, // Provider cannot be changed after creation
            Settings = requestModel.Settings,
            IsActive = requestModel.IsActive,
            DateCreated = existing.DateCreated
        };

        try
        {
            await _connectionService.SaveConnectionAsync(connection, cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return InvalidSettings(ex.Message);
        }
    }
}
