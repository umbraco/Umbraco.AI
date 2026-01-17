using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Extensions;
using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for updating prompts.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdatePromptController : PromptControllerBase
{
    private readonly IAiPromptService _aiPromptService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public UpdatePromptController(IAiPromptService aiPromptService, IUmbracoMapper umbracoMapper)
    {
        _aiPromptService = aiPromptService;
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
        AiPrompt? existing = await _aiPromptService.GetPromptAsync(promptIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return PromptNotFound();
        }

        AiPrompt prompt = _umbracoMapper.Map(model, existing);

        await _aiPromptService.SavePromptAsync(prompt, cancellationToken);
        return Ok();
    }
}
