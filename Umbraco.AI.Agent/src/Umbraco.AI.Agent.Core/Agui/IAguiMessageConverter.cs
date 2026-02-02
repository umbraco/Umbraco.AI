using Microsoft.Extensions.AI;
using Umbraco.AI.Agui.Models;

namespace Umbraco.AI.Agent.Core.Agui;

/// <summary>
/// Converts between AG-UI messages and Microsoft.Extensions.AI chat messages.
/// </summary>
public interface IAguiMessageConverter
{
    /// <summary>
    /// Converts AG-UI messages to M.E.AI chat messages.
    /// </summary>
    /// <param name="messages">The AG-UI messages to convert.</param>
    /// <returns>A list of chat messages suitable for the AI model.</returns>
    /// <remarks>
    /// Agent instructions are NOT included here - they are handled by AIAgentBoundChatClient.
    /// Context is handled separately via <see cref="IAguiContextConverter"/>.
    /// </remarks>
    List<ChatMessage> ConvertToChatMessages(IEnumerable<AguiMessage>? messages);

    /// <summary>
    /// Converts a single AG-UI message to an M.E.AI chat message.
    /// </summary>
    /// <param name="message">The AG-UI message to convert.</param>
    /// <returns>The converted chat message.</returns>
    ChatMessage ConvertToChatMessage(AguiMessage message);

    /// <summary>
    /// Converts an M.E.AI chat message to an AG-UI message.
    /// </summary>
    /// <param name="chatMessage">The chat message to convert.</param>
    /// <returns>The converted AG-UI message.</returns>
    AguiMessage ConvertFromChatMessage(ChatMessage chatMessage);
}
