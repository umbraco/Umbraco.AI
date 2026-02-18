using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Published after an AITest is saved (not cancelable).
/// </summary>
public sealed class AITestSavedNotification : AIEntitySavedNotification<AITest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AITestSavedNotification"/> class.
    /// </summary>
    /// <param name="entity">The test that was saved.</param>
    /// <param name="messages">Event messages from the save operation.</param>
    public AITestSavedNotification(AITest entity, EventMessages messages)
        : base(entity, messages)
    {
    }
}
