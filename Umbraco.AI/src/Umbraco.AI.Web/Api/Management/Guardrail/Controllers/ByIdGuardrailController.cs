using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Controller to get a guardrail by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdGuardrailController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdGuardrailController"/> class.
    /// </summary>
    public ByIdGuardrailController(IAIGuardrailService guardrailService, IUmbracoMapper umbracoMapper)
    {
        _guardrailService = guardrailService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a guardrail by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the guardrail.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail details.</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GuardrailResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuardrailById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var guardrail = await _guardrailService.GetGuardrailAsync(id, cancellationToken);
        if (guardrail is null)
        {
            return GuardrailNotFound();
        }

        return Ok(_umbracoMapper.Map<GuardrailResponseModel>(guardrail));
    }
}
