using Umbraco.AI.Core.Settings;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Cancels deletion of a profile that is configured as a default chat or embedding profile in settings.
/// </summary>
internal sealed class AIProfileDeletingNotificationHandler
    : INotificationAsyncHandler<AIProfileDeletingNotification>
{
    private readonly IAISettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileDeletingNotificationHandler"/> class.
    /// </summary>
    public AIProfileDeletingNotificationHandler(IAISettingsService settingsService)
        => _settingsService = settingsService;

    /// <inheritdoc />
    public async Task HandleAsync(AIProfileDeletingNotification notification, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);

        if (settings.DefaultChatProfileId == notification.EntityId ||
            settings.DefaultEmbeddingProfileId == notification.EntityId)
        {
            notification.Messages.Add(new EventMessage(
                "Profile in use",
                "Profile is configured as a default chat or embedding profile in settings.",
                EventMessageType.Error));
            notification.Cancel = true;
        }
    }
}
