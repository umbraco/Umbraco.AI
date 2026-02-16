using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published after an AIContext is saved (not cancelable).
/// </summary>
public sealed class AIContextSavedNotification : AIEntitySavedNotification<AIContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The context that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AIContextSavedNotification(AIContext entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
