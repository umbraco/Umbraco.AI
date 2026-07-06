using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Published after an inline image-generation execution completes (not cancelable).
/// </summary>
/// <remarks>
/// Contains execution results including duration and success status for telemetry and logging.
/// </remarks>
public sealed class AIImageGenerationExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIImageGenerationExecutedNotification"/> class.
    /// </summary>
    /// <param name="generationId">The deterministic generation ID.</param>
    /// <param name="alias">The generation alias.</param>
    /// <param name="name">The generation display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="messages">Event messages from the execution.</param>
    public AIImageGenerationExecutedNotification(
        Guid generationId,
        string alias,
        string name,
        Guid? profileId,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        GenerationId = generationId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
    }

    /// <summary>
    /// Gets the deterministic generation ID derived from the alias.
    /// </summary>
    public Guid GenerationId { get; }

    /// <summary>
    /// Gets the generation alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the generation display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the profile ID, or null if using the default image-generation profile.
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
