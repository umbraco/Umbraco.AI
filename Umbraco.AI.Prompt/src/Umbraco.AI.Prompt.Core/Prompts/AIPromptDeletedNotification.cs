using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published after an AIPrompt is deleted (not cancelable).
/// </summary>
public sealed class AIPromptDeletedNotification : AIEntityDeletedNotification<AIPrompt>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the prompt that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AIPromptDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
