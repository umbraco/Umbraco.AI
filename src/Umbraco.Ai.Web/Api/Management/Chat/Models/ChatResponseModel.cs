using System.ComponentModel.DataAnnotations;

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
    public ChatUsageModel? Usage { get; set; }
}

/// <summary>
/// Usage statistics for a chat completion.
/// </summary>
public class ChatUsageModel
{
    /// <summary>
    /// The number of tokens in the input.
    /// </summary>
    public long? InputTokens { get; set; }

    /// <summary>
    /// The number of tokens in the output.
    /// </summary>
    public long? OutputTokens { get; set; }

    /// <summary>
    /// The total number of tokens used.
    /// </summary>
    public long? TotalTokens { get; set; }
}
