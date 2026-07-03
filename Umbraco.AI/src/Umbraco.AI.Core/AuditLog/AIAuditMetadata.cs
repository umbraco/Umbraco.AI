using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Extracts audit metadata (declared via <see cref="Constants.ContextKeys.LogKeys"/>) from the
/// ambient runtime context. Used by capabilities whose LogKeys live in the runtime context
/// (chat, speech-to-text). Embedding reads its LogKeys from the options bag instead.
/// </summary>
internal static class AIAuditMetadata
{
    public static IReadOnlyDictionary<string, string>? ExtractFromRuntimeContext(AIRuntimeContext? context)
    {
        if (context?.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys) != true)
        {
            return null;
        }

        return logKeys!.ToDictionary(
            key => key,
            key => context!.GetValue<object?>(key)?.ToString() ?? string.Empty);
    }
}
