using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Agent.Core.McpPoc;
using Umbraco.AI.Agent.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Web.Api.Management.McpPoc;

/// <summary>
/// PROOF OF CONCEPT ONLY. Lists MCP servers and starts a shared-account sign-in.
/// Signed-in backoffice users only (inherits the management API policy).
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "McpPoc")]
[UmbracoAIVersionedManagementApiRoute("mcp-poc")]
public class McpPocController : UmbracoAIAgentManagementControllerBase
{
    private readonly IMcpPocSignInService _signInService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public McpPocController(IMcpPocSignInService signInService, IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _signInService = signInService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <summary>
    /// Lists the configured servers and whether each is signed in.
    /// </summary>
    [HttpGet("servers")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<McpPocServerStatus>), StatusCodes.Status200OK)]
    public IActionResult GetServers() => Ok(_signInService.GetServerStatuses());

    /// <summary>
    /// Starts a sign-in and returns the address to open in a popup.
    /// </summary>
    [HttpPost("servers/{alias}/connect")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Connect(string alias, CancellationToken cancellationToken)
    {
        if (_signInService.GetServer(alias) is null)
        {
            return NotFound();
        }

        var user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        var redirectUri = $"{Request.Scheme}://{Request.Host}{McpPocConstants.CallbackPath}";

        try
        {
            var url = await _signInService.StartSignInAsync(
                alias,
                redirectUri,
                user?.Email ?? user?.Username ?? "unknown",
                cancellationToken);
            return Ok(new { authorizationUrl = url });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new ProblemDetails { Title = "Could not start sign-in", Detail = ex.Message });
        }
    }
}
