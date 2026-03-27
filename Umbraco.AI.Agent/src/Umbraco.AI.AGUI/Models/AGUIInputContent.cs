using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Base class for AG-UI input content parts.
/// Supports the AG-UI multimodal messages draft where message content can be
/// either a string or an array of content parts.
/// </summary>
/// <remarks>
/// AG-UI Specification: https://docs.ag-ui.com/drafts/multimodal-messages
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AGUITextInputContent), "text")]
[JsonDerivedType(typeof(AGUIBinaryInputContent), "binary")]
public abstract class AGUIInputContent;
