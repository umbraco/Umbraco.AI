using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Agent.Web.Api.Management.Prompt.Models;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Agent.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for retrieving a prompt by ID or alias.
/// </summary>
[ApiVersion("1.0")]
public class ByIdOrAliasPromptController : PromptControllerBase
{
    private readonly IAiAgentService _AiAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public ByIdOrAliasPromptController(IAiAgentService AiAgentService, IUmbracoMapper umbracoMapper)
    {
        _AiAgentService = AiAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets a prompt by its ID or alias.
    /// </summary>
    /// <param name="promptIdOrAlias">The prompt ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt if found.</returns>
    [HttpGet($"{{{nameof(promptIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PromptResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPromptByIdOrAlias(
        IdOrAlias promptIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var prompt = await _AiAgentService.GetPromptAsync(promptIdOrAlias, cancellationToken);
        if (prompt is null)
        {
            return PromptNotFound();
        }

        return Ok(_umbracoMapper.Map<PromptResponseModel>(prompt));
    }
}
