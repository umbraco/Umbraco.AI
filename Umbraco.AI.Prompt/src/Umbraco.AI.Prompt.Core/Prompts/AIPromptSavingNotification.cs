using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published before an AIPrompt is saved (cancelable).
/// </summary>
public sealed class AIPromptSavingNotification : AIEntitySavingNotification<AIPrompt>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The prompt being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIPromptSavingNotification(AIPrompt entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
