using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Published after an AITest is deleted (not cancelable).
/// </summary>
public sealed class AITestDeletedNotification : AIEntityDeletedNotification<AITest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestDeletedNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the test that was deleted.</param>
    /// <param name="messages">Event messages from the delete operation.</param>
    public AITestDeletedNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
