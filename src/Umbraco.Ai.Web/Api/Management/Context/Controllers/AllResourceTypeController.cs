using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Context.ResourceTypes;
using Umbraco.Ai.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to get all registered context resource types.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllResourceTypeController : ContextControllerBase
{
    private readonly AiContextResourceTypeCollection _resourceTypes;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllResourceTypeController"/> class.
    /// </summary>
    public AllResourceTypeController(
        AiContextResourceTypeCollection resourceTypes,
        IUmbracoMapper umbracoMapper)
    {
        _resourceTypes = resourceTypes;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all registered context resource types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered resource types.</returns>
    [HttpGet("resource-types")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ResourceTypeItemResponseModel>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<ResourceTypeItemResponseModel>>> GetAllResourceTypes(
        CancellationToken cancellationToken = default)
    {
        var resourceTypes = _umbracoMapper.MapEnumerable<IAiContextResourceType, ResourceTypeItemResponseModel>(_resourceTypes);
        return Task.FromResult<ActionResult<IEnumerable<ResourceTypeItemResponseModel>>>(Ok(resourceTypes));
    }
}
