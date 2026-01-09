using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Extensions;
using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for executing prompts.
/// </summary>
[ApiVersion("1.0")]
public class ExecutePromptController : PromptControllerBase
{
    private readonly IAiPromptService _promptService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public ExecutePromptController(IAiPromptService promptService, IUmbracoMapper mapper)
    {
        _promptService = promptService;
        _mapper = mapper;
    }

    /// <summary>
    /// Executes a prompt and returns the AI response.
    /// </summary>
    /// <param name="promptIdOrAlias">The prompt ID (GUID) or alias (string).</param>
    /// <param name="requestModel">The execution request containing context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result containing the AI response.</returns>
    [HttpPost($"{{{nameof(promptIdOrAlias)}}}/execute")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PromptExecutionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecutePrompt(
        IdOrAlias promptIdOrAlias,
        [FromBody] PromptExecutionRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        // Resolve prompt ID from IdOrAlias
        var promptId = await _promptService.TryGetPromptIdAsync(promptIdOrAlias, cancellationToken);
        if (!promptId.HasValue)
        {
            return PromptNotFound();
        }

        try
        {
            var request = _mapper.Map<AiPromptExecutionRequest>(requestModel)!;
            var result = await _promptService.ExecutePromptAsync(promptId.Value, request, cancellationToken);
            return Ok(_mapper.Map<PromptExecutionResponseModel>(result));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return PromptNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Prompt execution failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
