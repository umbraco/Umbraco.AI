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
    /// <param name="capability">Optional capability to filter connections by (e.g., "Chat", "Embedding").</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of connections.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<ConnectionItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<ConnectionItemResponseModel>>> GetConnections(
        string? filter = null,
        string? providerId = null,
        string? capability = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<AiConnection> connections;
        int total;

        // If capability filter is provided, use the capability-based query
        if (!string.IsNullOrEmpty(capability) && Enum.TryParse<AiCapability>(capability, ignoreCase: true, out var cap))
        {
            var capabilityConnections = await _connectionService.GetConnectionsByCapabilityAsync(cap, cancellationToken);
            var connectionsList = capabilityConnections.ToList();

            // Apply additional filters if provided
            if (!string.IsNullOrEmpty(filter))
            {
                connectionsList = connectionsList.Where(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(providerId))
            {
                connectionsList = connectionsList.Where(c => c.ProviderId == providerId).ToList();
            }

            total = connectionsList.Count;
            connections = connectionsList.Skip(skip).Take(take);
        }
        else
        {
            (connections, total) = await _connectionService.GetConnectionsPagedAsync(filter, providerId, skip, take, cancellationToken);
        }

        var viewModel = new PagedViewModel<ConnectionItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AiConnection, ConnectionItemResponseModel>(connections)
        };

        return Ok(viewModel);
    }
}