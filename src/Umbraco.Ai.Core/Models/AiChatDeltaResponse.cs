namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Represents a chunk of generated chat message from an AI chat completion operation.
/// </summary>
public sealed class AiChatDeltaResponse
{
    /// <summary>
    /// The role of the message being generated (typically Assistant).
    /// </summary>
    public AiChatRole? Role { get; init; }
    
    /// <summary>
    /// The generated text chunk for the assistant's message.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Indicates whether this is the final chunk of the chat response.
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// The finish reason, if this is the final chunk.
    /// Examples: "stop" (natural completion), "length" (max tokens reached), "content_filter" (content was filtered)
    /// </summary>
    public string? FinishReason { get; init; }
}
