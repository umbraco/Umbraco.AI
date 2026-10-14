using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Extensions;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Controller for executing prompts.
/// </summary>
[ApiVersion("1.0")]
public class ExecutePromptController : PromptControllerBase
{
    private readonly IAIPromptService _promptService;
    private readonly IUmbracoMapper _mapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public ExecutePromptController(IAIPromptService promptService, IUmbracoMapper mapper)
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
        // Resolve prompt ID from IdOrAlias. An ID is returned without a lookup, so confirm the prompt
        // exists here; failures during execution are then never about the prompt itself.
        var promptId = await _promptService.TryGetPromptIdAsync(promptIdOrAlias, cancellationToken);
        if (!promptId.HasValue || await _promptService.GetPromptAsync(promptId.Value, cancellationToken) is null)
        {
            return PromptNotFound();
        }

        try
        {
            var request = _mapper.Map<AIPromptExecutionRequest>(requestModel)!;
            var result = await _promptService.ExecutePromptAsync(promptId.Value, request, cancellationToken);
            return Ok(_mapper.Map<PromptExecutionResponseModel>(result));
        }
        catch (InvalidOperationException ex)
        {
            // Covers a missing default profile, profile, connection or provider as well as scope and
            // notification failures.
            return ExecutionFailed(ex.Message);
        }
        catch (AIProviderException ex)
        {
            return ExecutionFailed(ex.UserMessage);
        }
    }

    // The backoffice only shows a problem's detail when it has a type; without one the user sees a
    // generic error message.
    private BadRequestObjectResult ExecutionFailed(string detail) => BadRequest(new ProblemDetails
    {
        Type = "Error",
        Title = "Prompt execution failed",
        Detail = detail,
        Status = StatusCodes.Status400BadRequest
    });
}
