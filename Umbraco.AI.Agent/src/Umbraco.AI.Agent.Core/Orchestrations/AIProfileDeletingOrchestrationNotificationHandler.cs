using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Cancels deletion of a profile that is still referenced by one or more orchestrations.
/// </summary>
internal sealed class AIProfileDeletingOrchestrationNotificationHandler
    : INotificationAsyncHandler<AIProfileDeletingNotification>
{
    private readonly IAIOrchestrationService _orchestrationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileDeletingOrchestrationNotificationHandler"/> class.
    /// </summary>
    public AIProfileDeletingOrchestrationNotificationHandler(IAIOrchestrationService orchestrationService)
        => _orchestrationService = orchestrationService;

    /// <inheritdoc />
    public async Task HandleAsync(AIProfileDeletingNotification notification, CancellationToken cancellationToken)
    {
        if (await _orchestrationService.OrchestrationsExistWithProfileAsync(notification.EntityId, cancellationToken))
        {
            notification.Messages.Add(new EventMessage(
                "Profile in use",
                "Profile is in use by one or more orchestrations.",
                EventMessageType.Error));
            notification.Cancel = true;
        }
    }
}
