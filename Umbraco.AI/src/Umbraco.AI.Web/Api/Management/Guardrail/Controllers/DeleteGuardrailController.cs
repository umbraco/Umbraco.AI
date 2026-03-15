using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Controller to delete a guardrail.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class DeleteGuardrailController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteGuardrailController"/> class.
    /// </summary>
    public DeleteGuardrailController(IAIGuardrailService guardrailService)
    {
        _guardrailService = guardrailService;
    }

    /// <summary>
    /// Delete a guardrail.
    /// </summary>
    /// <param name="id">The unique identifier of the guardrail to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGuardrail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _guardrailService.DeleteGuardrailAsync(id, cancellationToken);
        if (!deleted)
        {
            return GuardrailNotFound();
        }

        return Ok();
    }
}
