namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Represents the result of an AI chat completion operation.
/// </summary>
public sealed class AiChatResponse : AiResponseBase
{
    /// <summary>
    /// The generated message from the AI assistant.
    /// </summary>
    public required AiChatMessage Message { get; init; }

    /// <summary>
    /// The reason the chat completion finished, if provided by the model.
    /// Examples: "stop" (natural completion), "length" (max tokens reached), "content_filter" (content was filtered)
    /// </summary>
    public string? FinishReason { get; init; }
}
