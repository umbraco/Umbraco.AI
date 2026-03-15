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
/// Controller to get all guardrails.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllGuardrailController : GuardrailControllerBase
{
    private readonly IAIGuardrailService _guardrailService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllGuardrailController"/> class.
    /// </summary>
    public AllGuardrailController(IAIGuardrailService guardrailService, IUmbracoMapper umbracoMapper)
    {
        _guardrailService = guardrailService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all guardrails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of guardrails.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<GuardrailItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GuardrailItemResponseModel>>> GetAllGuardrails(
        CancellationToken cancellationToken = default)
    {
        var guardrails = await _guardrailService.GetAllGuardrailsAsync(cancellationToken);
        var items = _umbracoMapper.MapEnumerable<AIGuardrail, GuardrailItemResponseModel>(guardrails);
        return Ok(items);
    }
}
