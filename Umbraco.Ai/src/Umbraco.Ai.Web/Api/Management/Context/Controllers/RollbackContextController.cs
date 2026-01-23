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
/// Controller to rollback a context to a previous version.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class RollbackContextController : ContextControllerBase
{
    private readonly IAiContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackContextController"/> class.
    /// </summary>
    public RollbackContextController(
        IAiContextService contextService,
        IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Rollback a context to a previous version.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier (GUID) or alias of the context.</param>
    /// <param name="snapshotVersion">The version number to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context at the new version (after rollback).</returns>
    [HttpPost($"{{{nameof(contextIdOrAlias)}}}/versions/{{{nameof(snapshotVersion)}:int}}/rollback")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ContextResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RollbackContextToVersion(
        [FromRoute] IdOrAlias contextIdOrAlias,
        [FromRoute] int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var context = await _contextService.GetContextAsync(contextIdOrAlias, cancellationToken);
        if (context is null)
        {
            return ContextNotFound();
        }

        try
        {
            var rolledBackContext = await _contextService.RollbackContextAsync(context.Id, snapshotVersion, cancellationToken);
            return Ok(_umbracoMapper.Map<ContextResponseModel>(rolledBackContext));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Version"))
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {snapshotVersion} was not found for this context."));
        }
    }
}
