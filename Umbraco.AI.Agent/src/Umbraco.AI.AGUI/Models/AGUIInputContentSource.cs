using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Source of media/document content. Discriminated on <c>type</c>: either inline data
/// (base64) or a URL reference.
/// </summary>
/// <remarks>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts"/>.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AGUIInputContentDataSource), "data")]
[JsonDerivedType(typeof(AGUIInputContentUrlSource), "url")]
public abstract class AGUIInputContentSource
{
    /// <summary>
    /// MIME type of the content. Required on <see cref="AGUIInputContentDataSource"/>,
    /// optional on <see cref="AGUIInputContentUrlSource"/> (just a hint).
    /// </summary>
    public string? GetMimeType() => this switch
    {
        AGUIInputContentDataSource d => d.MimeType,
        AGUIInputContentUrlSource u => u.MimeType,
        _ => null,
    };
}
