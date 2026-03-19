using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Binary content part for AG-UI multimodal messages.
/// Represents file attachments such as images, PDFs, or other binary data.
/// </summary>
/// <remarks>
/// <para>
/// At least one of <see cref="Data"/>, <see cref="Url"/>, or <see cref="Id"/> must be provided.
/// </para>
/// <para>
/// AG-UI Specification: https://docs.ag-ui.com/drafts/multimodal-messages
/// </para>
/// </remarks>
public sealed class AGUIBinaryInputContent : AGUIInputContent
{
    /// <summary>
    /// Gets or sets the MIME type of the binary content (e.g., "image/png", "application/pdf").
    /// </summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded binary data.
    /// Used for initial file uploads from the frontend.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the URL where the binary content can be retrieved.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the identifier for server-stored content.
    /// Used after the first turn to reference previously uploaded files.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the original filename.
    /// </summary>
    [JsonPropertyName("filename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filename { get; set; }

    /// <summary>
    /// Gets or sets the resolved binary data bytes.
    /// This is populated by the file processor after resolving base64 data or id references.
    /// Not serialized to JSON — used internally for converter processing.
    /// </summary>
    [JsonIgnore]
    public byte[]? ResolvedData { get; set; }
}
