using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Cancels deletion of a profile that is still referenced by one or more prompts.
/// </summary>
internal sealed class AIProfileDeletingPromptNotificationHandler
    : INotificationAsyncHandler<AIProfileDeletingNotification>
{
    private readonly IAIPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileDeletingPromptNotificationHandler"/> class.
    /// </summary>
    public AIProfileDeletingPromptNotificationHandler(IAIPromptService promptService)
        => _promptService = promptService;

    /// <inheritdoc />
    public async Task HandleAsync(AIProfileDeletingNotification notification, CancellationToken cancellationToken)
    {
        if (await _promptService.PromptsExistWithProfileAsync(notification.EntityId, cancellationToken))
        {
            notification.Messages.Add(new EventMessage(
                "Profile in use",
                "Profile is in use by one or more prompts.",
                EventMessageType.Error));
            notification.Cancel = true;
        }
    }
}
