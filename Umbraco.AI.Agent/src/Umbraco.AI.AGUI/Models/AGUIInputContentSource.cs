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
public abstract class AGUIInputContentSource;
