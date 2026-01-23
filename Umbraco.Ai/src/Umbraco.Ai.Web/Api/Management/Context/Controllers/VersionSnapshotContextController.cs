using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to get a specific version snapshot of a context.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class VersionSnapshotContextController : ContextControllerBase
{
    private readonly IAiContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionSnapshotContextController"/> class.
    /// </summary>
    public VersionSnapshotContextController(
        IAiContextService contextService,
        IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a specific version snapshot of a context.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier (GUID) or alias of the context.</param>
    /// <param name="snapshotVersion">The version number to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context at the specified version.</returns>
    [HttpGet($"{{{nameof(contextIdOrAlias)}}}/versions/{{{nameof(snapshotVersion)}:int}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ContextResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContextVersionSnapshot(
        [FromRoute] IdOrAlias contextIdOrAlias,
        [FromRoute] int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var context = await _contextService.GetContextAsync(contextIdOrAlias, cancellationToken);
        if (context is null)
        {
            return ContextNotFound();
        }

        var snapshot = await _contextService.GetContextVersionSnapshotAsync(context.Id, snapshotVersion, cancellationToken);
        if (snapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {snapshotVersion} was not found for this context."));
        }

        return Ok(_umbracoMapper.Map<ContextResponseModel>(snapshot));
    }
}
