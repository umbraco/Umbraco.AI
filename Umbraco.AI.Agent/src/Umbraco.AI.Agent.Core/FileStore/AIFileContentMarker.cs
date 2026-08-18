namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// The well-known <c>AIContent.AdditionalProperties</c> key marking a content part's bytes as already
/// durably stored in <see cref="IAIFileStore"/> under this file id (scoped to the current thread id).
/// </summary>
/// <remarks>
/// A consumer that persists chat history (e.g. a <c>ChatHistoryProvider</c> backing a persisted
/// conversation) reads this to avoid writing the bytes a second time — it stores a small reference
/// content part instead, tagged with the same key, and rehydrates the real bytes back from
/// <see cref="IAIFileStore"/> when history is replayed to a model. This keeps the file store the single
/// place file bytes live; nothing else should hold a second, independently-aging copy of them.
/// </remarks>
public static class AIFileContentMarker
{
    /// <summary>
    /// The <c>AIContent.AdditionalProperties</c> key. The value is the file id (a <see cref="string"/>)
    /// as returned by <see cref="IAIFileStore.StoreAsync"/>.
    /// </summary>
    public const string FileIdPropertyKey = "umbraco-ai:fileId";
}
