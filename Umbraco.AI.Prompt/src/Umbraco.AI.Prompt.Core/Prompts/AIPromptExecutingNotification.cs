using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published before an AIPrompt is executed (cancelable).
/// </summary>
public sealed class AIPromptExecutingNotification : CancelableNotification
{
    public AIPromptExecutingNotification(AIPrompt prompt, AIPromptExecutionRequest request, EventMessages messages)
        : base(messages)
    {
        Prompt = prompt;
        Request = request;
    }

    /// <summary>
    /// Gets the prompt being executed.
    /// </summary>
    public AIPrompt Prompt { get; }

    /// <summary>
    /// Gets the execution request context.
    /// </summary>
    public AIPromptExecutionRequest Request { get; }
}
