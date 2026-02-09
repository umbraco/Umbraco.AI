using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Web.Api.Management.Tool.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Tool.Controllers;

/// <summary>
/// Controller to get all registered tool scopes.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllToolScopeController : ToolControllerBase
{
    private readonly AIToolScopeCollection _toolScopes;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllToolScopeController"/> class.
    /// </summary>
    public AllToolScopeController(AIToolScopeCollection toolScopes, IUmbracoMapper umbracoMapper)
    {
        _toolScopes = toolScopes;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all registered tool scopes.
    /// </summary>
    /// <remarks>
    /// Tool scopes define permission boundaries for agent tools. Each scope represents
    /// a category of operations (e.g., content-read, media-write, search).
    /// Name and description are localized on the frontend using the convention:
    /// - Name: uaiToolScope_{scopeId}Label
    /// - Description: uaiToolScope_{scopeId}Description
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered tool scopes.</returns>
    [HttpGet("scopes")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ToolScopeItemResponseModel>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<ToolScopeItemResponseModel>>> GetAllToolScopes(
        CancellationToken cancellationToken = default)
    {
        var toolScopes = _umbracoMapper.MapEnumerable<IAIToolScope, ToolScopeItemResponseModel>(_toolScopes
            .OrderBy(x => x.Domain)
            .ThenBy(x => x.Id));
        return Task.FromResult<ActionResult<IEnumerable<ToolScopeItemResponseModel>>>(Ok(toolScopes));
    }
}
