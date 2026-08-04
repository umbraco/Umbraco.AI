using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Cancels deletion of a project that still owns one or more conversations. Conversations must be moved
/// to another project (or deleted) first, so a project delete can never leave conversations with a
/// dangling project reference. Mirrors the connection/profile "in use" guard in Umbraco.AI.Core.
/// </summary>
internal sealed class AIProjectDeletingNotificationHandler
    : INotificationAsyncHandler<AIProjectDeletingNotification>
{
    private readonly IAIConversationService _conversationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProjectDeletingNotificationHandler"/> class.
    /// </summary>
    public AIProjectDeletingNotificationHandler(IAIConversationService conversationService)
        => _conversationService = conversationService;

    /// <inheritdoc />
    public async Task HandleAsync(AIProjectDeletingNotification notification, CancellationToken cancellationToken)
    {
        if (await _conversationService.ConversationsExistInProjectAsync(notification.EntityId, cancellationToken))
        {
            notification.Messages.Add(new EventMessage(
                "Project in use",
                "Project is in use by one or more conversations.",
                EventMessageType.Error));
            notification.Cancel = true;
        }
    }
}
