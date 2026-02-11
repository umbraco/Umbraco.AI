using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving all agent surfaces.
/// </summary>
[ApiVersion("1.0")]
public class AllAgentSurfaceController : AgentControllerBase
{
    private readonly AIAgentSurfaceCollection _surfaceCollection;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllAgentSurfaceController(AIAgentSurfaceCollection surfaceCollection, IUmbracoMapper umbracoMapper)
    {
        _surfaceCollection = surfaceCollection;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all registered agent surfaces.
    /// </summary>
    /// <remarks>
    /// Surfaces are defined by add-on packages and categorize agents for specific purposes.
    /// Name and description are localized on the frontend using the convention:
    /// - Name: uaiAgentSurface_{surfaceId}Label
    /// - Description: uaiAgentSurface_{surfaceId}Description
    /// </remarks>
    /// <returns>List of registered surfaces.</returns>
    [HttpGet("surfaces")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<AgentSurfaceItemResponseModel>), StatusCodes.Status200OK)]
    public IActionResult GetAgentSurfaces()
    {
        var surfaces = _surfaceCollection.ToList();
        var models = _umbracoMapper.MapEnumerable<IAIAgentSurface, AgentSurfaceItemResponseModel>(surfaces);
        return Ok(models);
    }
}
