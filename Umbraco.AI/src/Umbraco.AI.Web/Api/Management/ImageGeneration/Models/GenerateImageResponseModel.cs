namespace Umbraco.AI.Web.Api.Management.ImageGeneration.Models;

/// <summary>
/// Response model for an image-generation request.
/// </summary>
public class GenerateImageResponseModel
{
    /// <summary>
    /// The generated images.
    /// </summary>
    public IReadOnlyList<GeneratedImageModel> Images { get; init; } = [];

    /// <summary>
    /// Optional token usage reported by the provider.
    /// </summary>
    public ImageGenerationUsageModel? Usage { get; init; }
}

/// <summary>
/// A single generated image, returned as base64 data and/or a URL.
/// </summary>
public class GeneratedImageModel
{
    /// <summary>
    /// The base64-encoded image data, when the provider returned inline data.
    /// </summary>
    public string? Data { get; init; }

    /// <summary>
    /// The image URL, when the provider returned a hosted/URI image.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// The media type of the image (e.g. "image/png").
    /// </summary>
    public string? MediaType { get; init; }
}

/// <summary>
/// Token usage details for an image-generation request.
/// </summary>
public class ImageGenerationUsageModel
{
    /// <summary>Input token count, if reported.</summary>
    public long? InputTokens { get; init; }

    /// <summary>Output token count, if reported.</summary>
    public long? OutputTokens { get; init; }

    /// <summary>Total token count, if reported.</summary>
    public long? TotalTokens { get; init; }
}
