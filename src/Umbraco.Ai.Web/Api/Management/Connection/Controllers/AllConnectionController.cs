using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
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
    /// <param name="filter">Optional filter to search by name (case-insensitive contains).</param>
    /// <param name="providerId">Optional provider ID to filter connections.</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of connections.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<ConnectionItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<ConnectionItemResponseModel>>> GetAllConnections(
        string? filter = null,
        string? providerId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (connections, total) = await _connectionService.GetConnectionsPagedAsync(filter, providerId, skip, take, cancellationToken);

        var viewModel = new PagedViewModel<ConnectionItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AiConnection, ConnectionItemResponseModel>(connections)
        };

        return Ok(viewModel);
    }
}