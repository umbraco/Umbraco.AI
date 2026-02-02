using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to get a context by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdOrAliasContextController : ContextControllerBase
{
    private readonly IAIContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdOrAliasContextController"/> class.
    /// </summary>
    public ByIdOrAliasContextController(IAIContextService contextService, IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a context by its ID or alias.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier (GUID) or alias of the context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context details.</returns>
    [HttpGet($"{{{nameof(contextIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ContextResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContextByIdOrAlias(
        [FromRoute] IdOrAlias contextIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var context = await _contextService.GetContextAsync(contextIdOrAlias, cancellationToken);
        if (context is null)
        {
            return ContextNotFound();
        }

        return Ok(_umbracoMapper.Map<ContextResponseModel>(context));
    }
}
