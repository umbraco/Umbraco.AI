using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Web.Api.Management.KnowledgeSets.Mapping;
using Umbraco.AI.Web.Api.Management.KnowledgeSets.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Controllers;

/// <summary>
/// Controller to get a knowledge set by ID, including its items.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdKnowledgeSetController : KnowledgeSetControllerBase
{
    private readonly AIKnowledgeSetCollection _knowledgeSets;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdKnowledgeSetController"/> class.
    /// </summary>
    public ByIdKnowledgeSetController(AIKnowledgeSetCollection knowledgeSets, IUmbracoMapper umbracoMapper)
    {
        _knowledgeSets = knowledgeSets;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a knowledge set by its ID, including its items and their full content.
    /// </summary>
    /// <param name="id">The unique identifier of the knowledge set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The knowledge set details.</returns>
    [HttpGet($"{{{nameof(id)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(KnowledgeSetDetailResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKnowledgeSetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var knowledgeSet = _knowledgeSets.GetById(id);
        if (knowledgeSet is null)
        {
            return KnowledgeSetNotFound();
        }

        // IAIKnowledgeSet.GetItemsAsync is async but IUmbracoMapper map actions are synchronous, so the
        // items are resolved here and passed through the mapper context (see KnowledgeSetMapDefinition).
        var items = await knowledgeSet.GetItemsAsync(cancellationToken);
        var responseModel = _umbracoMapper.Map<KnowledgeSetDetailResponseModel>(
            knowledgeSet,
            ctx => ctx.Items[KnowledgeSetMapDefinition.ItemsKey] = items)!;

        return Ok(responseModel);
    }
}
