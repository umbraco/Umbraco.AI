using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for deleting prompts.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeletePromptController : PromptControllerBase
{
    private readonly IAIPromptService _aiPromptService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public DeletePromptController(IAIPromptService aiPromptService)
    {
        _aiPromptService = aiPromptService;
    }

    /// <summary>
    /// Deletes a prompt.
    /// </summary>
    /// <param name="promptIdOrAlias">The prompt ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete($"{{{nameof(promptIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePrompt(
        IdOrAlias promptIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var promptId = await _aiPromptService.TryGetPromptIdAsync(promptIdOrAlias, cancellationToken);
        if (promptId is null)
        {
            return PromptNotFound();
        }

        var deleted = await _aiPromptService.DeletePromptAsync(promptId.Value, cancellationToken);
        if (!deleted)
        {
            return PromptNotFound();
        }

        return NoContent();
    }
}
