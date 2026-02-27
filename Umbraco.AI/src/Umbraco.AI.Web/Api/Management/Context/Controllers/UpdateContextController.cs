using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to update an existing context.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateContextController : ContextControllerBase
{
    private readonly IAIContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateContextController"/> class.
    /// </summary>
    public UpdateContextController(
        IAIContextService contextService,
        IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update an existing context.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier or alias of the context to update.</param>
    /// <param name="requestModel">The updated context data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut($"{{{nameof(contextIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContext(
        IdOrAlias contextIdOrAlias,
        UpdateContextRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var existing = await _contextService.GetContextAsync(contextIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return ContextNotFound();
        }

        // Check for duplicate alias if alias is being changed
        if (existing.Alias != requestModel.Alias)
        {
            var existingByAlias = await _contextService.GetContextByAliasAsync(requestModel.Alias, cancellationToken);
            if (existingByAlias is not null && existingByAlias.Id != existing.Id)
            {
                return ContextOperationStatusResult(ContextOperationStatus.DuplicateAlias);
            }
        }

        AIContext context = _umbracoMapper.Map(requestModel, existing);
        await _contextService.SaveContextAsync(context, cancellationToken);
        return Ok();
    }
}
