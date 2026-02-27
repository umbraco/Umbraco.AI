using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published after an AIPrompt is executed (not cancelable).
/// </summary>
public sealed class AIPromptExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptExecutedNotification"/> class.
    /// </summary>
    /// <param name="prompt">The prompt that was executed.</param>
    /// <param name="request">The execution request context.</param>
    /// <param name="result">The execution result with response and usage data.</param>
    /// <param name="messages">Event messages from the execution operation.</param>
    public AIPromptExecutedNotification(
        AIPrompt prompt,
        AIPromptExecutionRequest request,
        AIPromptExecutionResult result,
        EventMessages messages)
    {
        Prompt = prompt;
        Request = request;
        Result = result;
        Messages = messages;
    }

    /// <summary>
    /// Gets the prompt that was executed.
    /// </summary>
    public AIPrompt Prompt { get; }

    /// <summary>
    /// Gets the execution request context.
    /// </summary>
    public AIPromptExecutionRequest Request { get; }

    /// <summary>
    /// Gets the execution result with response and usage data.
    /// </summary>
    public AIPromptExecutionResult Result { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}
