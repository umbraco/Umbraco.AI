using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Cancels deletion of a connection that is still referenced by one or more profiles.
/// </summary>
internal sealed class AIConnectionDeletingNotificationHandler
    : INotificationAsyncHandler<AIConnectionDeletingNotification>
{
    private readonly IAIProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionDeletingNotificationHandler"/> class.
    /// </summary>
    public AIConnectionDeletingNotificationHandler(IAIProfileService profileService)
        => _profileService = profileService;

    /// <inheritdoc />
    public async Task HandleAsync(AIConnectionDeletingNotification notification, CancellationToken cancellationToken)
    {
        if (await _profileService.ProfilesExistWithConnectionAsync(notification.EntityId, cancellationToken))
        {
            notification.Messages.Add(new EventMessage(
                "Connection in use",
                "Connection is in use by one or more profiles.",
                EventMessageType.Error));
            notification.Cancel = true;
        }
    }
}
