using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Management.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Chat.Models;

/// <summary>
/// Request model for chat completion.
/// </summary>
public class ChatRequestModel
{
    /// <summary>
    /// The profile to use for chat completion, specified by ID or alias.
    /// If not specified, the default chat profile will be used.
    /// </summary>
    public IdOrAlias? ProfileIdOrAlias { get; set; }

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
