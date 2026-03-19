using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Text content part for AG-UI multimodal messages.
/// </summary>
/// <remarks>
/// AG-UI Specification: https://docs.ag-ui.com/drafts/multimodal-messages
/// </remarks>
public sealed class AGUITextInputContent : AGUIInputContent
{
    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
