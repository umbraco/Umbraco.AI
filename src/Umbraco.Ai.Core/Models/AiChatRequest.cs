namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Represents a request for an AI chat completion.
/// </summary>
public sealed class AiChatRequest : AiRequestBase
{
    /// <summary>
    /// The messages that make up the chat conversation.
    /// </summary>
    public IList<AiChatMessage> Messages { get; init; } = new List<AiChatMessage>();
}