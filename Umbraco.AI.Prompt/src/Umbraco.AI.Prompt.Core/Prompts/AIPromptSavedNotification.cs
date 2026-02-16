using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published after an AIPrompt is saved (not cancelable).
/// </summary>
public sealed class AIPromptSavedNotification : AIEntitySavedNotification<AIPrompt>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The prompt that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIPromptSavedNotification(AIPrompt entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
