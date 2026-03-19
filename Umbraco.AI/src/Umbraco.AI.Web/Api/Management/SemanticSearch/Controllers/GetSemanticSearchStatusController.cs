using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.AI.Web.Api.Management.SemanticSearch.Models;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.SemanticSearch.Controllers;

/// <summary>
/// Controller to retrieve semantic search index status.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class GetSemanticSearchStatusController : SemanticSearchControllerBase
{
    private readonly IAISemanticSearchService _semanticSearchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSemanticSearchStatusController"/> class.
    /// </summary>
    public GetSemanticSearchStatusController(IAISemanticSearchService semanticSearchService)
    {
        _semanticSearchService = semanticSearchService;
    }

    /// <summary>
    /// Gets the current status of the semantic search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index status including document count and profile information.</returns>
    [HttpGet("status")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SemanticSearchStatusResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken = default)
    {
        var status = await _semanticSearchService.GetIndexStatusAsync(cancellationToken);

        return Ok(new SemanticSearchStatusResponseModel
        {
            TotalIndexed = status.TotalIndexed,
            ProfileId = status.ProfileId,
            ModelId = status.ModelId
        });
    }
}
