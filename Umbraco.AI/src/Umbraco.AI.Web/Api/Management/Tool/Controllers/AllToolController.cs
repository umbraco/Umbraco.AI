using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Tools;
using Umbraco.AI.Web.Api.Management.Tool.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Tool.Controllers;

/// <summary>
/// Controller to get all registered tools.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllToolController : ToolControllerBase
{
    private readonly AIToolCollection _tools;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllToolController"/> class.
    /// </summary>
    public AllToolController(AIToolCollection tools, IUmbracoMapper umbracoMapper)
    {
        _tools = tools;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all registered tools.
    /// </summary>
    /// <remarks>
    /// Returns all available tools that can be used by AI agents. Tools are grouped by scope
    /// and can be filtered based on permissions. System tools are always included and cannot
    /// be disabled.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered tools.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ToolItemResponseModel>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<ToolItemResponseModel>>> GetAllTools(
        CancellationToken cancellationToken = default)
    {
        // Get all user-configurable tools (exclude system tools)
        var tools = _umbracoMapper.MapEnumerable<IAITool, ToolItemResponseModel>(_tools
            .GetUserTools()
            .OrderBy(x => x.ScopeId)
            .ThenBy(x => x.Name));
        return Task.FromResult<ActionResult<IEnumerable<ToolItemResponseModel>>>(Ok(tools));
    }
}
