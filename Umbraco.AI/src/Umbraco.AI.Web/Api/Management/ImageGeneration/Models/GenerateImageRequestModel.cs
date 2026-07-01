namespace Umbraco.AI.Web.Api.Management.ImageGeneration.Models;

/// <summary>
/// Request model for generating images from a text prompt (with optional maskless edit).
/// </summary>
public class GenerateImageRequestModel
{
    /// <summary>
    /// The text prompt describing the desired image(s).
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Optional profile ID or alias to use. If omitted, the default image-generation profile is used.
    /// </summary>
    public string? ProfileIdOrAlias { get; init; }

    /// <summary>
    /// Optional number of images to generate.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Optional image size as "{width}x{height}" (e.g. "1024x1024").
    /// </summary>
    public string? Size { get; init; }

    /// <summary>
    /// Optional response format: "url", "data", or "hosted".
    /// </summary>
    public string? ResponseFormat { get; init; }

    /// <summary>
    /// Optional original images to edit (maskless edit). Masked outpainting is not exposed over REST —
    /// use the C# <c>GetService</c> escape hatch for that.
    /// </summary>
    public IReadOnlyList<ImageInputModel>? OriginalImages { get; init; }
}

/// <summary>
/// A base64-encoded input image for maskless editing.
/// </summary>
public class ImageInputModel
{
    /// <summary>
    /// The base64-encoded image data (no data-URI prefix).
    /// </summary>
    public required string Data { get; init; }

    /// <summary>
    /// The media type of the image (e.g. "image/png").
    /// </summary>
    public required string MediaType { get; init; }
}
