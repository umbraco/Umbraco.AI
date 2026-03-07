using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Controllers;

/// <summary>
/// Controller for retrieving an orchestration by ID or alias.
/// </summary>
[ApiVersion("1.0")]
public class ByIdOrAliasOrchestrationController : OrchestrationControllerBase
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public ByIdOrAliasOrchestrationController(IAIOrchestrationService orchestrationService, IUmbracoMapper umbracoMapper)
    {
        _orchestrationService = orchestrationService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets an orchestration by its ID or alias.
    /// </summary>
    /// <param name="orchestrationIdOrAlias">The orchestration ID (GUID) or alias (string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The orchestration if found.</returns>
    [HttpGet($"{{{nameof(orchestrationIdOrAlias)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(OrchestrationResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrchestrationByIdOrAlias(
        IdOrAlias orchestrationIdOrAlias,
        CancellationToken cancellationToken = default)
    {
        var orchestration = await _orchestrationService.GetOrchestrationAsync(orchestrationIdOrAlias, cancellationToken);
        if (orchestration is null)
        {
            return OrchestrationNotFound();
        }

        return Ok(_umbracoMapper.Map<OrchestrationResponseModel>(orchestration));
    }
}
