using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Context;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to create a new context.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreateContextController : ContextControllerBase
{
    private readonly IAiContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateContextController"/> class.
    /// </summary>
    public CreateContextController(
        IAiContextService contextService,
        IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Create a new context.
    /// </summary>
    /// <param name="requestModel">The context to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created context ID.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContext(
        CreateContextRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate alias
        var existingByAlias = await _contextService.GetContextByAliasAsync(requestModel.Alias, cancellationToken);
        if (existingByAlias is not null)
        {
            return ContextOperationStatusResult(ContextOperationStatus.DuplicateAlias);
        }

        AiContext context = _umbracoMapper.Map<AiContext>(requestModel)!;
        var created = await _contextService.SaveContextAsync(context, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdOrAliasContextController.GetContextByIdOrAlias),
            nameof(ByIdOrAliasContextController).Replace("Controller", string.Empty),
            new { contextIdOrAlias = created.Id },
            created.Id.ToString());
    }
}
