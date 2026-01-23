using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to get version history for a context.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class VersionHistoryContextController : ContextControllerBase
{
    private readonly IAiContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionHistoryContextController"/> class.
    /// </summary>
    public VersionHistoryContextController(
        IAiContextService contextService,
        IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get version history for a context.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier (GUID) or alias of the context.</param>
    /// <param name="skip">Number of versions to skip (for pagination).</param>
    /// <param name="take">Number of versions to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version history for the context.</returns>
    [HttpGet($"{{{nameof(contextIdOrAlias)}}}/versions")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EntityVersionHistoryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContextVersionHistory(
        [FromRoute] IdOrAlias contextIdOrAlias,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var context = await _contextService.GetContextAsync(contextIdOrAlias, cancellationToken);
        if (context is null)
        {
            return ContextNotFound();
        }

        var versions = await _contextService.GetContextVersionHistoryAsync(context.Id, cancellationToken: cancellationToken);
        var versionList = versions.ToList();

        // Map to response models
        var responseVersions = versionList
            .Skip(skip)
            .Take(take)
            .Select(v => _umbracoMapper.Map<EntityVersionResponseModel>(v)!)
            .ToList();

        return Ok(new EntityVersionHistoryResponseModel
        {
            CurrentVersion = context.Version,
            TotalVersions = versionList.Count,
            Versions = responseVersions
        });
    }
}
