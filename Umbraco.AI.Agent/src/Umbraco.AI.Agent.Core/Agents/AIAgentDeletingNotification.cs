using Umbraco.AI.Agent.Core.Models;
using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Published before an AIAgent is deleted (cancelable).
/// </summary>
public sealed class AIAgentDeletingNotification : AIEntityDeletingNotification<AIAgent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDeletingNotification"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the agent being deleted.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIAgentDeletingNotification(Guid entityId, EventMessages messages)
        : base(entityId, messages)
    { }
}
