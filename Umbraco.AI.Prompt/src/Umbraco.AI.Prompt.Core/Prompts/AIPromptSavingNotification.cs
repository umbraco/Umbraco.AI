using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published before an AIPrompt is saved (cancelable).
/// </summary>
public sealed class AIPromptSavingNotification : AIEntitySavingNotification<AIPrompt>
{
    public AIPromptSavingNotification(AIPrompt entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
