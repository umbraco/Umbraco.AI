using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published before an AIContext is saved (cancelable).
/// </summary>
public sealed class AIContextSavingNotification : AIEntitySavingNotification<AIContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The context being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIContextSavingNotification(AIContext entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
