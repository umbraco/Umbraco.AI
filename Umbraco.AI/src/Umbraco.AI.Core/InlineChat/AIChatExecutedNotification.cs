using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.InlineChat;

/// <summary>
/// Published after an inline chat execution completes (not cancelable).
/// </summary>
/// <remarks>
/// Contains execution results including duration and success status for telemetry and logging.
/// </remarks>
public sealed class AIChatExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIChatExecutedNotification"/> class.
    /// </summary>
    /// <param name="chatId">The deterministic chat ID.</param>
    /// <param name="alias">The chat alias.</param>
    /// <param name="name">The chat display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="messages">Event messages from the execution.</param>
    public AIChatExecutedNotification(
        Guid chatId,
        string alias,
        string name,
        Guid? profileId,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        ChatId = chatId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
    }

    /// <summary>
    /// Gets the deterministic chat ID derived from the alias.
    /// </summary>
    public Guid ChatId { get; }

    /// <summary>
    /// Gets the chat alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the chat display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the profile ID, or null if using the default chat profile.
    /// </summary>
    public Guid? ProfileId { get; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets whether the execution completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}
