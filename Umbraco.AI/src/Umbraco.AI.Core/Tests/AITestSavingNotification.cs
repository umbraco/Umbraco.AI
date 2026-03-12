using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Published before an AITest is saved (cancelable).
/// </summary>
public sealed class AITestSavingNotification : AIEntitySavingNotification<AITest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestSavingNotification"/> class.
    /// </summary>
    /// <param name="entity">The test being saved.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AITestSavingNotification(AITest entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
