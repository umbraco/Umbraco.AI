using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Published after an inline speech-to-text execution completes (not cancelable).
/// </summary>
/// <remarks>
/// Contains execution results including duration and success status for telemetry and logging.
/// </remarks>
public sealed class AISpeechToTextExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISpeechToTextExecutedNotification"/> class.
    /// </summary>
    /// <param name="transcriptionId">The deterministic transcription ID.</param>
    /// <param name="alias">The transcription alias.</param>
    /// <param name="name">The transcription display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="messages">Event messages from the execution.</param>
    public AISpeechToTextExecutedNotification(
        Guid transcriptionId,
        string alias,
        string name,
        Guid? profileId,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        TranscriptionId = transcriptionId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
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
