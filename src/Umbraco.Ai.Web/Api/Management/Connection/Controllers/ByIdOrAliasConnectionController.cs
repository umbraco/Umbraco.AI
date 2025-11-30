using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get a connection by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdOrAliasConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdOrAliasConnectionController"/> class.
    /// </summary>
    public ByIdOrAliasConnectionController(IAiConnectionService connectionService, IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a connection by its ID or alias.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection details.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConnectionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConnectionByIdOrAlias(
        IdOrAlias connectionIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(connectionIdOrAlias, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        return Ok(_umbracoMapper.Map<ConnectionResponseModel>(connection));
    }
}
