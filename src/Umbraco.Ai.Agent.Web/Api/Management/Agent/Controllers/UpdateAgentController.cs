using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Agent.Web.Api.Management.Prompt.Models;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Agent.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for updating Agents.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdatePromptController : PromptControllerBase
{
    private readonly IAiAgentService _AiAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public UpdatePromptController(IAiAgentService AiAgentService, IUmbracoMapper umbracoMapper)
    {
        _AiAgentService = AiAgentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Updates an existing prompt.
    /// </summary>
    /// <param name="promptIdOrAlias">The prompt ID (GUID) or alias (string).</param>
    /// <param name="model">The prompt update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated prompt.</returns>
    [HttpPut($"{{{nameof(promptIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePrompt(
        IdOrAlias promptIdOrAlias,
        [FromBody] UpdatePromptRequestModel model,
        CancellationToken cancellationToken = default)
    {
        AiAgent? existing = await _AiAgentService.GetPromptAsync(promptIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return PromptNotFound();
        }

        AiAgent prompt = _umbracoMapper.Map(model, existing);

        await _AiAgentService.SavePromptAsync(prompt, cancellationToken);
        return Ok();
    }
}
