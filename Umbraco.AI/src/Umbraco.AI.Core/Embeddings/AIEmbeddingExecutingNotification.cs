using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// Published before an inline embedding execution begins (cancelable).
/// </summary>
public sealed class AIEmbeddingExecutingNotification : CancelableNotification
{
    public AIEmbeddingExecutingNotification(
        Guid embeddingId,
        string alias,
        string name,
        Guid? profileId,
        EventMessages messages)
        : base(messages)
    {
        EmbeddingId = embeddingId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
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
}
