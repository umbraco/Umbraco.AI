using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Chat.Models;

/// <summary>
/// Response model for chat completion.
/// </summary>
public class ChatResponseModel
{
    /// <summary>
    /// The generated response message.
    /// </summary>
    [Required]
    public ChatMessageModel Message { get; set; } = new() { Role = "assistant", Content = string.Empty };

    /// <summary>
    /// The finish reason (e.g., "stop", "length", "tool_calls").
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Usage statistics for the request.
    /// </summary>
    public UsageModel? Usage { get; set; }
}
