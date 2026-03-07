using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Controller for updating orchestrations.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class UpdateOrchestrationController : OrchestrationControllerBase
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public UpdateOrchestrationController(IAIOrchestrationService orchestrationService, IUmbracoMapper umbracoMapper)
    {
        _orchestrationService = orchestrationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Updates an existing orchestration.
    /// </summary>
    /// <param name="orchestrationIdOrAlias">The orchestration ID (GUID) or alias (string).</param>
    /// <param name="model">The orchestration update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK if successful.</returns>
    [HttpPut($"{{{nameof(orchestrationIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOrchestration(
        IdOrAlias orchestrationIdOrAlias,
        [FromBody] UpdateOrchestrationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        AIOrchestration? existing = await _orchestrationService.GetOrchestrationAsync(orchestrationIdOrAlias, cancellationToken);
        if (existing is null)
        {
            return OrchestrationNotFound();
        }

        AIOrchestration orchestration = _umbracoMapper.Map(model, existing);

        await _orchestrationService.SaveOrchestrationAsync(orchestration, cancellationToken);
        return Ok();
    }
}
