using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Base class for AG-UI input content parts. Discriminated on <c>type</c>.
/// </summary>
/// <remarks>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts"/>. Spec defines five
/// content types: <c>text</c>, <c>image</c>, <c>audio</c>, <c>video</c>, <c>document</c>.
/// The <c>document</c> variant is the catch-all for non-media MIME types.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AGUITextInputContent), "text")]
[JsonDerivedType(typeof(AGUIImageInputContent), "image")]
[JsonDerivedType(typeof(AGUIAudioInputContent), "audio")]
[JsonDerivedType(typeof(AGUIVideoInputContent), "video")]
[JsonDerivedType(typeof(AGUIDocumentInputContent), "document")]
public abstract class AGUIInputContent;
