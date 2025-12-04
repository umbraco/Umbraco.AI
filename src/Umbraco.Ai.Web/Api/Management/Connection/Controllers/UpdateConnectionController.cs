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
using Umbraco.Cms.Core.Mapping;
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
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateConnectionController"/> class.
    /// </summary>
    public UpdateConnectionController(
        IAiConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
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

        AiConnection connection = _umbracoMapper.Map(requestModel, existing);

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
