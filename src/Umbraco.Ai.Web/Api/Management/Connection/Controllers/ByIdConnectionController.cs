using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get a connection by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdConnectionController"/> class.
    /// </summary>
    public ByIdConnectionController(IAiConnectionService connectionService, IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a connection by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection details.</returns>
    [HttpGet($"{{{nameof(id)}:guid}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConnectionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(id, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        return Ok(_umbracoMapper.Map<ConnectionResponseModel>(connection));
    }
}
