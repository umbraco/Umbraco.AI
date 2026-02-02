using System.ComponentModel.DataAnnotations;
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
    /// The content of the message.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;
}
