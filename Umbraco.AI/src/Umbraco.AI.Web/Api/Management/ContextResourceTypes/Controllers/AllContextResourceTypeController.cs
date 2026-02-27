using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Web.Api.Management.ContextResourceTypes.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.ContextResourceTypes.Controllers;

/// <summary>
/// Controller to get all registered context resource types.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllContextResourceTypeController : ContextResourceTypeControllerBase
{
    private readonly AIContextResourceTypeCollection _contextResourceTypes;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllContextResourceTypeController"/> class.
    /// </summary>
    public AllContextResourceTypeController(
        AIContextResourceTypeCollection contextResourceTypes,
        IUmbracoMapper umbracoMapper)
    {
        _contextResourceTypes = contextResourceTypes;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all registered context resource types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered resource types.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ContextResourceTypeResponseModel>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<ContextResourceTypeResponseModel>>> GetAllContextResourceTypes(
        CancellationToken cancellationToken = default)
    {
        var contextResourceTypes = _umbracoMapper.MapEnumerable<IAIContextResourceType, ContextResourceTypeResponseModel>(_contextResourceTypes);
        return Task.FromResult<ActionResult<IEnumerable<ContextResourceTypeResponseModel>>>(Ok(contextResourceTypes));
    }
}
