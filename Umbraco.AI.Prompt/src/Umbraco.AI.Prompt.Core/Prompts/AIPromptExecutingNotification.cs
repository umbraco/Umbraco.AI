using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published before an AIPrompt is executed (cancelable).
/// </summary>
public sealed class AIPromptExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptExecutingNotification"/> class.
    /// </summary>
    /// <param name="prompt">The prompt being executed.</param>
    /// <param name="request">The execution request context.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
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
