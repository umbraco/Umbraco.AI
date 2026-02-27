using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for creating prompts.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class CreatePromptController : PromptControllerBase
{
    private readonly IAIPromptService _aiPromptService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public CreatePromptController(IAIPromptService aiPromptService, IUmbracoMapper umbracoMapper)
    {
        _aiPromptService = aiPromptService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Creates a new prompt.
    /// </summary>
    /// <param name="model">The prompt creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created prompt.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PromptResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePrompt(
        [FromBody] CreatePromptRequestModel model,
        CancellationToken cancellationToken = default)
    {
        AIPrompt prompt = _umbracoMapper.Map<AIPrompt>(model)!;

        try
        {
            AIPrompt created = await _aiPromptService.SavePromptAsync(prompt, cancellationToken);

            return CreatedAtAction(
                nameof(ByIdOrAliasPromptController.GetPromptByIdOrAlias),
                "ByIdOrAliasPrompt",
                new { promptIdOrAlias = created.Id },
                created.Id.ToString());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return AliasAlreadyExists(model.Alias);
        }
    }
}
