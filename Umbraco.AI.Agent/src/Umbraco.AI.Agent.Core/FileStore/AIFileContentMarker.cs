using System.Text.Json;

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

    /// <summary>
    /// Reads a file id marker value that may be a plain <see cref="string"/> (built in-memory this
    /// request) or a <see cref="JsonElement"/> (loaded back from a JSON round trip — stored history,
    /// or an AG-UI request body model-bound by <c>System.Text.Json</c>, both deserialize untyped
    /// <c>object?</c> dictionary values this way, never as the original CLR type).
    /// </summary>
    public static bool TryGetFileId(object? value, out string fileId)
    {
        switch (value)
        {
            case string s when !string.IsNullOrEmpty(s):
                fileId = s;
                return true;
            case JsonElement { ValueKind: JsonValueKind.String } je when !string.IsNullOrEmpty(je.GetString()):
                fileId = je.GetString()!;
                return true;
            default:
                fileId = string.Empty;
                return false;
        }
    }
}
