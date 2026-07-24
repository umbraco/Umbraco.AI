using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Web.Api.Management.KnowledgeSets.Models;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Controllers;

/// <summary>
/// Controller to get the materialised content of a single knowledge set item by its key.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByKeyKnowledgeSetItemController : KnowledgeSetControllerBase
{
    private readonly AIKnowledgeSetCollection _knowledgeSets;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByKeyKnowledgeSetItemController"/> class.
    /// </summary>
    public ByKeyKnowledgeSetItemController(AIKnowledgeSetCollection knowledgeSets)
    {
        _knowledgeSets = knowledgeSets;
    }

    /// <summary>
    /// Get the content of a single item within a knowledge set, materialising it on demand.
    /// </summary>
    /// <param name="id">The unique identifier of the knowledge set.</param>
    /// <param name="key">The stable key of the item within the knowledge set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The item's key and its materialised markdown content.</returns>
    [HttpGet($"{{{nameof(id)}}}/item/{{{nameof(key)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(KnowledgeSetItemContentResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKnowledgeSetItemContent(
        string id,
        string key,
        CancellationToken cancellationToken = default)
    {
        var knowledgeSet = _knowledgeSets.GetById(id);
        if (knowledgeSet is null)
        {
            return KnowledgeSetNotFound();
        }

        var items = await knowledgeSet.GetItemsAsync(cancellationToken);
        var item = items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            return KnowledgeSetItemNotFound();
        }

        // Content is fetched lazily here — only when an admin actually views the item.
        var content = await item.GetContentAsync(cancellationToken);

        return Ok(new KnowledgeSetItemContentResponseModel
        {
            Key = item.Key,
            Content = content
        });
    }
}
