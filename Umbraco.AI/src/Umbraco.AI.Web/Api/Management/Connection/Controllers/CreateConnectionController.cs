using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to create a new connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateConnectionController : ConnectionControllerBase
{
    private readonly IAIConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateConnectionController"/> class.
    /// </summary>
    public CreateConnectionController(
        IAIConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Create a new connection.
    /// </summary>
    /// <param name="requestModel">The connection to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created connection ID.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateConnection(
        CreateConnectionRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        AIConnection connection = _umbracoMapper.Map<AIConnection>(requestModel)!;

        try
        {
            var created = await _connectionService.SaveConnectionAsync(connection, cancellationToken);
            return CreatedAtAction(
                nameof(ByIdOrAliasConnectionController.GetConnectionByIdOrAlias),
                nameof(ByIdOrAliasConnectionController).Replace("Controller", string.Empty),
                new { connectionIdOrAlias = created.Id },
                created.Id.ToString());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found in registry"))
        {
            return ProviderNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return InvalidSettings(ex.Message);
        }
    }
}
