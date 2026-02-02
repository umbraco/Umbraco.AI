using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Scopes;
using Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving all agent scopes.
/// </summary>
[ApiVersion("1.0")]
public class AllAgentScopeController : AgentControllerBase
{
    private readonly AiAgentScopeCollection _scopeCollection;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllAgentScopeController(AiAgentScopeCollection scopeCollection, IUmbracoMapper umbracoMapper)
    {
        _scopeCollection = scopeCollection;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all registered agent scopes.
    /// </summary>
    /// <remarks>
    /// Scopes are defined by add-on packages and categorize agents for specific purposes.
    /// Name and description are localized on the frontend using the convention:
    /// - Name: uaiAgentScope_{scopeId}Label
    /// - Description: uaiAgentScope_{scopeId}Description
    /// </remarks>
    /// <returns>List of registered scopes.</returns>
    [HttpGet("scopes")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<AgentScopeItemResponseModel>), StatusCodes.Status200OK)]
    public IActionResult GetAgentScopes()
    {
        var scopes = _scopeCollection.ToList();
        var models = _umbracoMapper.MapEnumerable<IAiAgentScope, AgentScopeItemResponseModel>(scopes);
        return Ok(models);
    }
}
