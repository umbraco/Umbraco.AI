using System.Drawing;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.ImageGeneration.Models;

#pragma warning disable MEAI001 // Image generation types are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Consumes the experimental image-generation service

namespace Umbraco.AI.Web.Api.Management.ImageGeneration.Controllers;

/// <summary>
/// Controller to generate images from a text prompt (with optional maskless edit).
/// </summary>
[ApiVersion("1.0")]
public class GenerateImageController : ImageGenerationControllerBase
{
    private readonly IAIImageGenerationService _imageGenerationService;
    private readonly IAIProfileService _profileService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateImageController"/> class.
    /// </summary>
    public GenerateImageController(
        IAIImageGenerationService imageGenerationService,
        IAIProfileService profileService,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _imageGenerationService = imageGenerationService;
        _profileService = profileService;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Generate one or more images from a text prompt.
    /// </summary>
    /// <param name="model">The image-generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated image(s) as base64 data and/or URLs.</returns>
    [HttpPost("generate")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(GenerateImageResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateImageRequestModel model,
        CancellationToken cancellationToken = default)
    {
        // Experimental gate: when the feature is off the capability does not exist as far as the API is concerned.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.ImageGeneration))
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.Prompt))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid prompt",
                Detail = "A prompt must be provided.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var profileId = model.ProfileIdOrAlias != null
                ? await _profileService.TryGetProfileIdAsync(IdOrAlias.Parse(model.ProfileIdOrAlias, null), cancellationToken)
                : null;

            var originalImages = MapOriginalImages(model.OriginalImages);

            var result = await _imageGenerationService.GenerateImagesAsync(
                b =>
                {
                    b.WithAlias("backoffice-image-generation");

                    if (profileId.HasValue)
                    {
                        b.WithProfile(profileId.Value);
                    }

                    var options = BuildOptions(model);
                    if (options is not null)
                    {
                        b.WithImageGenerationOptions(options);
                    }
                },
                model.Prompt,
                originalImages,
                cancellationToken);

            return Ok(MapResponse(result));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return ProfileNotFound();
        }
        catch (FormatException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid image data",
                Detail = "One or more original images were not valid base64.",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Image generation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static List<AIContent>? MapOriginalImages(IReadOnlyList<ImageInputModel>? originalImages)
    {
        if (originalImages is not { Count: > 0 })
        {
            return null;
        }

        return originalImages
            .Select(i => (AIContent)new DataContent(Convert.FromBase64String(i.Data), i.MediaType))
            .ToList();
    }

    private static ImageGenerationOptions? BuildOptions(GenerateImageRequestModel model)
    {
        var options = new ImageGenerationOptions();
        var hasOptions = false;

        if (model.Count.HasValue)
        {
            options.Count = model.Count;
            hasOptions = true;
        }

        if (ParseSize(model.Size) is { } size)
        {
            options.ImageSize = size;
            hasOptions = true;
        }

        if (ParseResponseFormat(model.ResponseFormat) is { } responseFormat)
        {
            options.ResponseFormat = responseFormat;
            hasOptions = true;
        }

        return hasOptions ? options : null;
    }

    private static GenerateImageResponseModel MapResponse(ImageGenerationResponse response)
    {
        var images = (response.Contents ?? [])
            .Select(MapContent)
            .OfType<GeneratedImageModel>()
            .ToList();

        var usage = response.Usage is { } u
            ? new ImageGenerationUsageModel
            {
                InputTokens = u.InputTokenCount,
                OutputTokens = u.OutputTokenCount,
                TotalTokens = u.TotalTokenCount
            }
            : null;

        return new GenerateImageResponseModel
        {
            Images = images,
            Usage = usage
        };
    }

    private static GeneratedImageModel? MapContent(AIContent content) => content switch
    {
        DataContent dataContent => new GeneratedImageModel
        {
            Data = Convert.ToBase64String(dataContent.Data.Span),
            MediaType = dataContent.MediaType
        },
        UriContent uriContent => new GeneratedImageModel
        {
            Url = uriContent.Uri.ToString(),
            MediaType = uriContent.MediaType
        },
        _ => null
    };

    private static Size? ParseSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return null;
        }

        var parts = size.Split('x', 'X', '×');
        if (parts.Length == 2
            && int.TryParse(parts[0].Trim(), out var width)
            && int.TryParse(parts[1].Trim(), out var height))
        {
            return new Size(width, height);
        }

        return null;
    }

    private static ImageGenerationResponseFormat? ParseResponseFormat(string? responseFormat)
        => responseFormat?.Trim().ToLowerInvariant() switch
        {
            "url" or "uri" => ImageGenerationResponseFormat.Uri,
            "data" or "base64" or "b64" or "b64_json" => ImageGenerationResponseFormat.Data,
            "hosted" => ImageGenerationResponseFormat.Hosted,
            _ => null
        };
}
