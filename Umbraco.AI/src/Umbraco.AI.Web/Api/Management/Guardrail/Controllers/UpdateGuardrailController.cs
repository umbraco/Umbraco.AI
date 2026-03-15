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
/// Controller to update an existing guardrail.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateGuardrailController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateGuardrailController"/> class.
    /// </summary>
    public UpdateGuardrailController(
        IAIGuardrailService guardrailService,
        IUmbracoMapper umbracoMapper)
    {
        _guardrailService = guardrailService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Update an existing guardrail.
    /// </summary>
    /// <param name="id">The unique identifier of the guardrail to update.</param>
    /// <param name="requestModel">The updated guardrail data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGuardrail(
        Guid id,
        UpdateGuardrailRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var existing = await _guardrailService.GetGuardrailAsync(id, cancellationToken);
        if (existing is null)
        {
            return GuardrailNotFound();
        }

        // Check for duplicate alias if alias is being changed
        if (existing.Alias != requestModel.Alias)
        {
            var aliasExists = await _guardrailService.GuardrailAliasExistsAsync(requestModel.Alias, id, cancellationToken);
            if (aliasExists)
            {
                return GuardrailOperationStatusResult(GuardrailOperationStatus.DuplicateAlias);
            }
        }

        AIGuardrail guardrail = _umbracoMapper.Map(requestModel, existing);
        await _guardrailService.UpdateGuardrailAsync(guardrail, cancellationToken);
        return Ok();
    }
}
