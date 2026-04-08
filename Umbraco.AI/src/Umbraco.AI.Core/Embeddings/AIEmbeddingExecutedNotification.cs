using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// Published after an inline embedding execution completes (not cancelable).
/// </summary>
public sealed class AIEmbeddingExecutedNotification : StatefulNotification
{
    public AIEmbeddingExecutedNotification(
        Guid embeddingId,
        string alias,
        string name,
        Guid? profileId,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        EmbeddingId = embeddingId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
    }

    /// <summary>
    /// Gets the deterministic embedding ID derived from the alias.
    /// </summary>
    public Guid EmbeddingId { get; }

    /// <summary>
    /// Gets the embedding alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the embedding display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the profile ID, or null if using the default embedding profile.
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
