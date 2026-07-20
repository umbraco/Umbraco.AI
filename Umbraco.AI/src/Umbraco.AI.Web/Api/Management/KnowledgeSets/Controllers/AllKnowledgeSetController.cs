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
/// Controller to get all installed knowledge sets.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllKnowledgeSetController : KnowledgeSetControllerBase
{
    private readonly AIKnowledgeSetCollection _knowledgeSets;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllKnowledgeSetController"/> class.
    /// </summary>
    public AllKnowledgeSetController(AIKnowledgeSetCollection knowledgeSets, IUmbracoMapper umbracoMapper)
    {
        _knowledgeSets = knowledgeSets;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all installed knowledge sets.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all installed knowledge sets.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<KnowledgeSetResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KnowledgeSetResponseModel>>> GetAllKnowledgeSets(
        CancellationToken cancellationToken = default)
    {
        var responseModels = new List<KnowledgeSetResponseModel>();

        foreach (var knowledgeSet in _knowledgeSets)
        {
            // IAIKnowledgeSet.GetItemsAsync is async but IUmbracoMapper map actions are synchronous, so the
            // item count is resolved here and passed through the mapper context (see KnowledgeSetMapDefinition).
            var items = await knowledgeSet.GetItemsAsync(cancellationToken);
            var responseModel = _umbracoMapper.Map<KnowledgeSetResponseModel>(
                knowledgeSet,
                ctx => ctx.Items[KnowledgeSetMapDefinition.ItemCountKey] = items.Count)!;

            responseModels.Add(responseModel);
        }

        return Ok(responseModels);
    }
}
