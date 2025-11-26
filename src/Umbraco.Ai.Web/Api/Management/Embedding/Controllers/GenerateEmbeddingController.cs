using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Web.Api.Management.Embedding.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Embedding.Controllers;

/// <summary>
/// Controller to generate embeddings.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class GenerateEmbeddingController : EmbeddingControllerBase
{
    private readonly IAiEmbeddingService _embeddingService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateEmbeddingController"/> class.
    /// </summary>
    public GenerateEmbeddingController(IAiEmbeddingService embeddingService, IUmbracoMapper umbracoMapper)
    {
        _embeddingService = embeddingService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Generate embeddings for the provided text values.
    /// </summary>
    /// <param name="requestModel">The embedding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated embeddings.</returns>
    [HttpPost("generate")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EmbeddingResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(
        GenerateEmbeddingRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var embeddings = requestModel.ProfileId.HasValue
                ? await _embeddingService.GenerateEmbeddingsAsync(
                    requestModel.ProfileId.Value,
                    requestModel.Values,
                    cancellationToken: cancellationToken)
                : await _embeddingService.GenerateEmbeddingsAsync(
                    requestModel.Values,
                    cancellationToken: cancellationToken);

            // Map embeddings with their index position
            var embeddingItems = embeddings
                .Select((e, i) =>
                {
                    var item = _umbracoMapper.Map<EmbeddingItemModel>(e)!;
                    item.Index = i;
                    return item;
                })
                .ToList();

            var response = new EmbeddingResponseModel
            {
                Embeddings = embeddingItems
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return ProfileNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Embedding generation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
