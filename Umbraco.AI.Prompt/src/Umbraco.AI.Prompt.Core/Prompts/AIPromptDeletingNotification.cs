using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published before an AIPrompt is deleted (cancelable).
/// </summary>
public sealed class AIPromptDeletingNotification : AIEntityDeletingNotification<AIPrompt>
{
    public AIPromptDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
