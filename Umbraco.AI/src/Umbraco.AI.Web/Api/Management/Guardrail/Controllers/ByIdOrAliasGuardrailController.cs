using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Controller to get a guardrail by ID or alias.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdOrAliasGuardrailController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdOrAliasGuardrailController"/> class.
    /// </summary>
    public ByIdOrAliasGuardrailController(IAIGuardrailService guardrailService, IUmbracoMapper umbracoMapper)
    {
        _guardrailService = guardrailService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a guardrail by its ID or alias.
    /// </summary>
    /// <param name="guardrailIdOrAlias">The unique identifier (GUID) or alias of the guardrail.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The guardrail details.</returns>
    [HttpGet($"{{{nameof(guardrailIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GuardrailResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuardrailByIdOrAlias(
        [FromRoute] IdOrAlias guardrailIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var guardrail = await _guardrailService.GetGuardrailAsync(guardrailIdOrAlias, cancellationToken);
        if (guardrail is null)
        {
            return GuardrailNotFound();
        }

        return Ok(_umbracoMapper.Map<GuardrailResponseModel>(guardrail));
    }
}
