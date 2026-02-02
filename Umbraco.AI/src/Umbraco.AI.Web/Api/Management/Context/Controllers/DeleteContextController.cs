using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to delete a context.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteContextController : ContextControllerBase
{
    private readonly IAiContextService _contextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteContextController"/> class.
    /// </summary>
    public DeleteContextController(IAiContextService contextService)
    {
        _contextService = contextService;
    }

    /// <summary>
    /// Delete a context.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier or alias of the context to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(contextIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContext(
        IdOrAlias contextIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        // Resolve to ID first since DeleteContextAsync requires Guid
        var contextId = await _contextService.TryGetContextIdAsync(contextIdOrAlias, cancellationToken);
        if (contextId is null)
        {
            return ContextNotFound();
        }

        var deleted = await _contextService.DeleteContextAsync(contextId.Value, cancellationToken);
        if (!deleted)
        {
            return ContextNotFound();
        }

        return Ok();
    }
}
