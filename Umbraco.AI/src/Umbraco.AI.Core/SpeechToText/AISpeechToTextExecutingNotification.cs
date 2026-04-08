using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Published before an inline speech-to-text execution begins (cancelable).
/// </summary>
/// <remarks>
/// Subscribers can inspect the transcription configuration and cancel execution by setting <see cref="CancelableNotification.Cancel"/>.
/// Cancellation reasons should be added to the <see cref="StatefulNotification.Messages"/> collection.
/// </remarks>
public sealed class AISpeechToTextExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISpeechToTextExecutingNotification"/> class.
    /// </summary>
    /// <param name="transcriptionId">The deterministic transcription ID.</param>
    /// <param name="alias">The transcription alias.</param>
    /// <param name="name">The transcription display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AISpeechToTextExecutingNotification(
        Guid transcriptionId,
        string alias,
        string name,
        Guid? profileId,
        EventMessages messages)
        : base(messages)
    {
        TranscriptionId = transcriptionId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
    }

    /// <summary>
    /// Gets the deterministic transcription ID derived from the alias.
    /// </summary>
    public Guid TranscriptionId { get; }

    /// <summary>
    /// Gets the transcription alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the transcription display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the profile ID, or null if using the default speech-to-text profile.
    /// </summary>
    public Guid? ProfileId { get; }
}
