using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Published before an inline image-generation execution begins (cancelable).
/// </summary>
/// <remarks>
/// Subscribers can inspect the generation configuration and cancel execution by setting
/// <see cref="CancelableNotification.Cancel"/>. Cancellation reasons should be added to the
/// <see cref="StatefulNotification.Messages"/> collection.
/// </remarks>
public sealed class AIImageGenerationExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIImageGenerationExecutingNotification"/> class.
    /// </summary>
    /// <param name="generationId">The deterministic generation ID.</param>
    /// <param name="alias">The generation alias.</param>
    /// <param name="name">The generation display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIImageGenerationExecutingNotification(
        Guid generationId,
        string alias,
        string name,
        Guid? profileId,
        EventMessages messages)
        : base(messages)
    {
        GenerationId = generationId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
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
}
