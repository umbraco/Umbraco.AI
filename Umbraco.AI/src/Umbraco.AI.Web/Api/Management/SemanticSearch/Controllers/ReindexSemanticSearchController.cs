using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.SemanticSearch.Controllers;

/// <summary>
/// Controller to trigger a full semantic search reindex.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ReindexSemanticSearchController : SemanticSearchControllerBase
{
    private readonly IAISemanticSearchService _semanticSearchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReindexSemanticSearchController"/> class.
    /// </summary>
    public ReindexSemanticSearchController(IAISemanticSearchService semanticSearchService)
    {
        _semanticSearchService = semanticSearchService;
    }

    /// <summary>
    /// Triggers a full rebuild of the semantic search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("reindex")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken = default)
    {
        await _semanticSearchService.ReindexAllAsync(cancellationToken);
        return Ok();
    }
}
