using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Embedding.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Embedding.Controllers;

/// <summary>
/// Controller to generate embeddings.
/// </summary>
[ApiVersion("1.0")]
public class GenerateEmbeddingController : EmbeddingControllerBase
{
    private readonly IAiEmbeddingService _embeddingService;
    private readonly IAiProfileService _profileService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateEmbeddingController"/> class.
    /// </summary>
    public GenerateEmbeddingController(
        IAiEmbeddingService embeddingService,
        IAiProfileService profileService,
        IUmbracoMapper umbracoMapper)
    {
        _embeddingService = embeddingService;
        _profileService = profileService;
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
    public async Task<IActionResult> GenerateEmbeddings(
        GenerateEmbeddingRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Resolve profile ID from IdOrAlias
            var profileId = requestModel.ProfileIdOrAlias != null
                ? await _profileService.TryGetProfileIdAsync(requestModel.ProfileIdOrAlias, cancellationToken)
                : null;

            var embeddings = profileId.HasValue
                ? await _embeddingService.GenerateEmbeddingsAsync(
                    profileId.Value,
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
