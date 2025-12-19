using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Web.Api.Management.Prompt.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Agent.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for creating Agents.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CreatePromptController : PromptControllerBase
{
    private readonly IAiAgentService _AiAgentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public CreatePromptController(IAiAgentService AiAgentService, IUmbracoMapper umbracoMapper)
    {
        _AiAgentService = AiAgentService;
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
        AiAgent prompt = _umbracoMapper.Map<AiAgent>(model)!;

        try
        {
            AiAgent created = await _AiAgentService.SavePromptAsync(prompt, cancellationToken);

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
