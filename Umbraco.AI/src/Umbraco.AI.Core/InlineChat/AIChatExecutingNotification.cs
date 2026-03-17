using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.InlineChat;

/// <summary>
/// Published before an inline chat execution begins (cancelable).
/// </summary>
/// <remarks>
/// Subscribers can inspect the chat configuration and cancel execution by setting <see cref="CancelableNotification.Cancel"/>.
/// Cancellation reasons should be added to the <see cref="StatefulNotification.Messages"/> collection.
/// </remarks>
public sealed class AIChatExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIChatExecutingNotification"/> class.
    /// </summary>
    /// <param name="chatId">The deterministic chat ID.</param>
    /// <param name="alias">The chat alias.</param>
    /// <param name="name">The chat display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIChatExecutingNotification(
        Guid chatId,
        string alias,
        string name,
        Guid? profileId,
        EventMessages messages)
        : base(messages)
    {
        ChatId = chatId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
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
}
