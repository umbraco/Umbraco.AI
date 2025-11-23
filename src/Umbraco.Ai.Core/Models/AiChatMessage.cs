namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Represents a message in an AI chat conversation.
/// </summary>
public sealed class AiChatMessage
{
    /// <summary>
    /// The role of the message in the conversation.
    /// </summary>
    public required AiChatRole Role { get; init; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public required string Text { get; init; }
}