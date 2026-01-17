using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Contexts.ResourceTypes;
using Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Controllers;

/// <summary>
/// Controller to get a context resource type by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdContextResourceTypeController : ContextResourceTypeControllerBase
{
    private readonly AiContextResourceTypeCollection _contextResourceTypes;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdContextResourceTypeController"/> class.
    /// </summary>
    public ByIdContextResourceTypeController(AiContextResourceTypeCollection contextResourceTypes, IUmbracoMapper umbracoMapper)
    {
        _contextResourceTypes = contextResourceTypes;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a context resource type by its ID including settings schema.
    /// </summary>
    /// <param name="id">The unique identifier of the context resource type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context resource type details.</returns>
    [HttpGet($"{{{nameof(id)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ContextResourceTypeResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetContextResourceTypeById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var contextResourceType = _contextResourceTypes.GetById(id);
        if (contextResourceType is null)
        {
            return Task.FromResult(ResourceTypeNotFound());
        }

        return Task.FromResult<IActionResult>(Ok(_umbracoMapper.Map<ContextResourceTypeResponseModel>(contextResourceType)));
    }
}
