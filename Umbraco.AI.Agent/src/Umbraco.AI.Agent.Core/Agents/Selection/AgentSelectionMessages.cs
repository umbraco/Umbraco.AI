using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Core.Agents.Selection;

/// <summary>
/// Shared message-extraction helpers for <see cref="IAIAgentSelector"/> implementations.
/// </summary>
/// <remarks>
/// Lives here (rather than as an internal member of one selector) so <see cref="LLMAgentSelector"/>
/// and <see cref="DecisionAgentSelector"/> share the same walk without one selector reaching into
/// the other's internals.
/// </remarks>
internal static class AgentSelectionMessages
{
    /// <summary>
    /// Returns the text of the last <see cref="ChatRole.User"/> message, or an empty string if there
    /// is none.
    /// </summary>
    public static string GetLastUserMessageText(IReadOnlyList<ChatMessage> messages)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i].Role == ChatRole.User)
            {
                return messages[i].Text ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
