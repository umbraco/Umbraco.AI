using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
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
    /// <param name="filter">Optional filter to search by name or alias (case-insensitive contains).</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of guardrails.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<GuardrailItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<GuardrailItemResponseModel>>> GetAllGuardrails(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (guardrails, total) = await _guardrailService.GetGuardrailsPagedAsync(
            filter,
            skip,
            take,
            cancellationToken);

        var viewModel = new PagedViewModel<GuardrailItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AIGuardrail, GuardrailItemResponseModel>(guardrails)
        };

        return Ok(viewModel);
    }
}
