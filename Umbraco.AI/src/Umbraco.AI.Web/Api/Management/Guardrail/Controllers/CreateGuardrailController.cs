using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Controller to create a new guardrail.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class CreateGuardrailController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGuardrailController"/> class.
    /// </summary>
    public CreateGuardrailController(
        IAIGuardrailService guardrailService,
        IUmbracoMapper umbracoMapper)
    {
        _guardrailService = guardrailService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Create a new guardrail.
    /// </summary>
    /// <param name="requestModel">The guardrail to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created guardrail ID.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGuardrail(
        CreateGuardrailRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate alias
        var existingByAlias = await _guardrailService.GetGuardrailByAliasAsync(requestModel.Alias, cancellationToken);
        if (existingByAlias is not null)
        {
            return GuardrailOperationStatusResult(GuardrailOperationStatus.DuplicateAlias);
        }

        AIGuardrail guardrail = _umbracoMapper.Map<AIGuardrail>(requestModel)!;
        var created = await _guardrailService.SaveGuardrailAsync(guardrail, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdOrAliasGuardrailController.GetGuardrailByIdOrAlias),
            nameof(ByIdOrAliasGuardrailController).Replace("Controller", string.Empty),
            new { guardrailIdOrAlias = created.Id },
            created.Id.ToString());
    }
}
