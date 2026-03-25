using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Chat.Models;

/// <summary>
/// Request model for chat completion.
/// </summary>
public class ChatRequestModel
{
    /// <summary>
    /// The chat messages to send.
    /// </summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<ChatMessageModel> Messages { get; set; } = [];
}

/// <summary>
/// Represents a chat message.
/// </summary>
public class ChatMessageModel
{
    /// <summary>
    /// The role of the message sender (system, user, assistant).
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The plain-text content of the message. Use <see cref="ContentParts"/> for multimodal content.
    /// </summary>
    [Obsolete("Use ContentParts instead. This property will be removed in a future version.")]
    public string? Content { get; set; }

    /// <summary>
    /// The multimodal content parts of the message (text and/or binary).
    /// When set, takes precedence over <see cref="Content"/>.
    /// </summary>
    public IReadOnlyList<ChatContentPartModel>? ContentParts { get; set; }
}

/// <summary>
/// Base class for multimodal chat content parts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextChatContentPartModel), "text")]
[JsonDerivedType(typeof(BinaryChatContentPartModel), "binary")]
public abstract class ChatContentPartModel;

/// <summary>
/// A text content part.
/// </summary>
public class TextChatContentPartModel : ChatContentPartModel
{
    /// <summary>
    /// The text content.
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// A binary content part (e.g., image, document).
/// </summary>
public class BinaryChatContentPartModel : ChatContentPartModel
{
    /// <summary>
    /// The MIME type of the binary data.
    /// </summary>
    [Required]
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// The base64-encoded binary data.
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The original filename, if available.
    /// </summary>
    public string? Filename { get; set; }
}
