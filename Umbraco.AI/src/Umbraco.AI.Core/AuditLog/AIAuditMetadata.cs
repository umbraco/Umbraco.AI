using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Extracts audit metadata (declared via <see cref="Constants.ContextKeys.LogKeys"/>) from the
/// ambient runtime context. Used by all capabilities (chat, embedding, speech-to-text) — LogKeys
/// always live in the runtime context, written there by the builders' WithAdditionalProperties.
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
