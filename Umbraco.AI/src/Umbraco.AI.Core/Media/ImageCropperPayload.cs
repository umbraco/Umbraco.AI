using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.Media;

/// <summary>
/// Minimal DTO representing the image cropper JSON payload stored on the
/// Umbraco <c>umbracoFile</c> property. Intentionally decoupled from
/// <c>Umbraco.Cms.Core.PropertyEditors.ValueConverters.ImageCropperValue</c>
/// so we can deserialise via System.Text.Json without dragging in CMS
/// internals or Newtonsoft attributes.
/// </summary>
internal sealed class ImageCropperPayload
{
    [JsonPropertyName("src")]
    public string? Src { get; set; }

    [JsonPropertyName("crops")]
    public List<ImageCropperCrop>? Crops { get; set; }
}

internal sealed class ImageCropperCrop
{
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("coordinates")]
    public ImageCropperCropCoordinates? Coordinates { get; set; }
}

internal sealed class ImageCropperCropCoordinates
{
    [JsonPropertyName("x1")]
    public decimal X1 { get; set; }

    [JsonPropertyName("y1")]
    public decimal Y1 { get; set; }

    [JsonPropertyName("x2")]
    public decimal X2 { get; set; }

    [JsonPropertyName("y2")]
    public decimal Y2 { get; set; }
}
