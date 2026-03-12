using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Published before an AITest is deleted (cancelable).
/// </summary>
public sealed class AITestDeletingNotification : AIEntityDeletingNotification<AITest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the test being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AITestDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    {
    }
}
