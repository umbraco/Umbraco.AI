using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Extensions;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for updating prompts.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class UpdatePromptController : PromptControllerBase
{
    private readonly IAIPromptService _aiPromptService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public UpdatePromptController(IAIPromptService aiPromptService, IUmbracoMapper umbracoMapper)
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
        AIPrompt? existing = await _aiPromptService.GetPromptAsync(promptIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return PromptNotFound();
        }

        AIPrompt prompt = _umbracoMapper.Map(model, existing);

        await _aiPromptService.SavePromptAsync(prompt, cancellationToken);
        return Ok();
    }
}
