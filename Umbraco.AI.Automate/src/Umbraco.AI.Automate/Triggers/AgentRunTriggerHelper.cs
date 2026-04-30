using Microsoft.Extensions.AI;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Shared helpers for agent run triggers.
/// </summary>
internal static class AgentRunTriggerHelper
{
    /// <summary>
    /// Returns the text of the most recent user message in <paramref name="chatMessages"/>,
    /// or an empty string if none is present.
    /// </summary>
    public static string GetLastUserPrompt(IReadOnlyList<ChatMessage> chatMessages)
    {
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            if (chatMessages[i].Role == ChatRole.User)
            {
                return chatMessages[i].Text ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
