using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Web.Api.Management.Connection.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get all connections.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllConnectionController"/> class.
    /// </summary>
    public AllConnectionController(IAiConnectionService connectionService, IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all connections.
    /// </summary>
    /// <param name="providerId">Optional provider ID to filter connections.</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of connections.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<ConnectionItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<ConnectionItemResponseModel>>> GetAll(
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var connections = await _connectionService.GetConnectionsAsync(providerId, cancellationToken);
        var connectionList = connections.ToList();

        var viewModel = new PagedViewModel<ConnectionItemResponseModel>
        {
            Total = connectionList.Count,
            Items = _umbracoMapper.MapEnumerable<AiConnection, ConnectionItemResponseModel>(
                connectionList.Skip(skip).Take(take))
        };

        return Ok(viewModel);
    }
}