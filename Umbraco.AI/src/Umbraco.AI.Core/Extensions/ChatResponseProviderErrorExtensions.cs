using Microsoft.Extensions.AI;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Detects a provider failure reported inside a <see cref="ChatResponse"/> rather than thrown.
/// </summary>
internal static class ChatResponseProviderErrorExtensions
{
    /// <summary>
    /// Returns the provider error the response ended on: an <see cref="ErrorContent"/> in the last
    /// assistant message with no text or function call after it. Null when the response ended normally,
    /// including when the model carried on past an earlier error.
    /// </summary>
    /// <remarks>
    /// Some providers report a failure (e.g. a rate limit hit on the final model call of a tool loop) as
    /// <see cref="ErrorContent"/> rather than by throwing, so the call itself returns normally.
    /// </remarks>
    public static ErrorContent? GetTerminalProviderError(this ChatResponse response)
    {
        var lastAssistant = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
        if (lastAssistant is null)
        {
            return null;
        }

        for (var i = lastAssistant.Contents.Count - 1; i >= 0; i--)
        {
            switch (lastAssistant.Contents[i])
            {
                case ErrorContent error:
                    return error;
                case TextContent text when !string.IsNullOrWhiteSpace(text.Text):
                case FunctionCallContent:
                    return null;
            }
        }

        return null;
    }
}
