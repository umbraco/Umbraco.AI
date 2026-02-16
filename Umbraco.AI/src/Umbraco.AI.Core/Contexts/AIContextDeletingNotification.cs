using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Published before an AIContext is deleted (cancelable).
/// </summary>
public sealed class AIContextDeletingNotification : AIEntityDeletingNotification<AIContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the context being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIContextDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
