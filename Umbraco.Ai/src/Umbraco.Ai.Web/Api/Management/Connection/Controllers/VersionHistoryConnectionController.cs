using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get version history for a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class VersionHistoryConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionHistoryConnectionController"/> class.
    /// </summary>
    public VersionHistoryConnectionController(
        IAiConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get version history for a connection.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier (GUID) or alias of the connection.</param>
    /// <param name="skip">Number of versions to skip (for pagination).</param>
    /// <param name="take">Number of versions to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version history for the connection.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}/versions")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EntityVersionHistoryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConnectionVersionHistory(
        [FromRoute] IdOrAlias connectionIdOrAlias,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionService.GetConnectionAsync(connectionIdOrAlias, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        var versions = await _connectionService.GetConnectionVersionHistoryAsync(connection.Id, cancellationToken: cancellationToken);
        var versionList = versions.ToList();

        // Map to response models
        var responseVersions = versionList
            .Skip(skip)
            .Take(take)
            .Select(v => _umbracoMapper.Map<EntityVersionResponseModel>(v)!)
            .ToList();

        return Ok(new EntityVersionHistoryResponseModel
        {
            CurrentVersion = connection.Version,
            TotalVersions = versionList.Count,
            Versions = responseVersions
        });
    }
}
