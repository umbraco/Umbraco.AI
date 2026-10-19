namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Reads the values a caller asked to have logged with an AI call.
/// </summary>
internal static class AIRuntimeContextLogValuesExtensions
{
    /// <summary>
    /// Returns the runtime context values named in <see cref="Constants.ContextKeys.LogKeys"/> (written by
    /// the builders' <c>WithAdditionalProperties</c>), as strings keyed by name. Null when no keys were
    /// declared or there is no context.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? GetLogValues(this AIRuntimeContext? context)
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
