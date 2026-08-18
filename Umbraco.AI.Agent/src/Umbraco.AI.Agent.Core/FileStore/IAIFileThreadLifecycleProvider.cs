namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// A vote on whether a file-store thread id still has a live backing record somewhere else in the
/// system (a persisted conversation, say). Consulted by the retention sweep before it ages out an old,
/// otherwise-untouched thread directory, so a long-lived conversation's attachments are not deleted just
/// because nobody has posted to it in a while.
/// </summary>
public enum AIFileThreadLifecycleStatus
{
    /// <summary>This provider does not recognize the thread id — it isn't one of its own.</summary>
    Unclaimed,

    /// <summary>This provider recognizes the thread id and its backing record still exists.</summary>
    Alive,

    /// <summary>This provider recognizes the thread id, but its backing record is gone.</summary>
    Gone,
}

/// <summary>
/// Registered via <c>IUmbracoBuilder.AIFileThreadLifecycleProviders()</c> by anything that stores
/// longer-lived records under a file-store thread id (<c>threadId := conversationId</c>, for example),
/// so the age-based retention sweep in <see cref="AIFileStore"/> can tell those apart from a plain,
/// short-lived chat thread that really is meant to expire on a fixed clock.
/// </summary>
public interface IAIFileThreadLifecycleProvider
{
    /// <summary>
    /// Reports whether <paramref name="threadId"/> belongs to this provider and, if so, whether its
    /// backing record still exists. Only called for thread directories already past the retention
    /// window's age — this is not a hot path.
    /// </summary>
    Task<AIFileThreadLifecycleStatus> GetStatusAsync(string threadId, CancellationToken cancellationToken = default);
}
