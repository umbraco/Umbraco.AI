using Umbraco.AI.Core.Models.Notifications;
using Umbraco.AI.Prompt.Core.Models;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Published after an AIPrompt is deleted (not cancelable).
/// </summary>
public sealed class AIPromptDeletedNotification : AIEntityDeletedNotification<AIPrompt>
{
    public AIPromptDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
