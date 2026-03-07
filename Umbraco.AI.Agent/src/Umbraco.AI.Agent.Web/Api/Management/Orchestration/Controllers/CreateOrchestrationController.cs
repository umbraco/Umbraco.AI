using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Controller for creating orchestrations.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class CreateOrchestrationController : OrchestrationControllerBase
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public CreateOrchestrationController(IAIOrchestrationService orchestrationService, IUmbracoMapper umbracoMapper)
    {
        _orchestrationService = orchestrationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Creates a new orchestration.
    /// </summary>
    /// <param name="model">The orchestration creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created orchestration ID.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(OrchestrationResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrchestration(
        [FromBody] CreateOrchestrationRequestModel model,
        CancellationToken cancellationToken = default)
    {
        AIOrchestration orchestration = _umbracoMapper.Map<AIOrchestration>(model)!;

        try
        {
            AIOrchestration created = await _orchestrationService.SaveOrchestrationAsync(orchestration, cancellationToken);

            return CreatedAtAction(
                nameof(ByIdOrAliasOrchestrationController.GetOrchestrationByIdOrAlias),
                "ByIdOrAliasOrchestration",
                new { orchestrationIdOrAlias = created.Id },
                created.Id.ToString());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return AliasAlreadyExists(model.Alias);
        }
    }
}
