using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Chat.Models;

/// <summary>
/// Response model for chat completion.
/// </summary>
public class ChatResponseModel
{
    /// <summary>
    /// The generated response message.
    /// </summary>
[Required]
#pragma warning disable CS0618 // Content is obsolete but still used for response serialization
    public ChatMessageModel Message { get; set; } = new() { Role = "assistant", Content = string.Empty };
#pragma warning restore CS0618

    /// <summary>
    /// The finish reason (e.g., "stop", "length", "tool_calls").
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Usage statistics for the request.
    /// </summary>
    public UsageModel? Usage { get; set; }
}
