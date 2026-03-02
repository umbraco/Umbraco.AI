using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Cancels deletion of a profile that is still referenced by one or more agents.
/// </summary>
internal sealed class AIProfileDeletingAgentNotificationHandler
    : INotificationAsyncHandler<AIProfileDeletingNotification>
{
    private readonly IAIAgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileDeletingAgentNotificationHandler"/> class.
    /// </summary>
    public AIProfileDeletingAgentNotificationHandler(IAIAgentService agentService)
        => _agentService = agentService;

    /// <inheritdoc />
    public async Task HandleAsync(AIProfileDeletingNotification notification, CancellationToken cancellationToken)
    {
        if (await _agentService.AgentsExistWithProfileAsync(notification.EntityId, cancellationToken))
        {
            notification.Messages.Add(new EventMessage(
                "Profile in use",
                "Profile is in use by one or more agents.",
                EventMessageType.Error));
            notification.Cancel = true;
        }
    }
}
