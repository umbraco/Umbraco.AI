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
/// Controller to get all registered context resource types.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllContextResourceTypeController : ContextResourceTypeControllerBase
{
    private readonly AiContextResourceTypeCollection _contextResourceTypes;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllContextResourceTypeController"/> class.
    /// </summary>
    public AllContextResourceTypeController(
        AiContextResourceTypeCollection contextResourceTypes,
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
        var contextResourceTypes = _umbracoMapper.MapEnumerable<IAiContextResourceType, ContextResourceTypeResponseModel>(_contextResourceTypes);
        return Task.FromResult<ActionResult<IEnumerable<ContextResourceTypeResponseModel>>>(Ok(contextResourceTypes));
    }
}
