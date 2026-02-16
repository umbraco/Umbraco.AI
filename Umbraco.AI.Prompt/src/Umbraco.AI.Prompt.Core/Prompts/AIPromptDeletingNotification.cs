using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published before an AIPrompt is deleted (cancelable).
/// </summary>
public sealed class AIPromptDeletingNotification : AIEntityDeletingNotification<AIPrompt>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the prompt being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIPromptDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
