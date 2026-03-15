using System.Text.Json;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration passed to a guardrail evaluator when evaluating content.
/// Wraps the rule's evaluator-specific configuration JSON.
/// </summary>
public sealed class AIGuardrailConfig
{
    /// <summary>
    /// Evaluator-specific configuration as a JSON element.
    /// </summary>
    public JsonElement? Config { get; init; }

    /// <summary>
    /// Deserializes the configuration to the specified type.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <returns>The deserialized configuration, or default if null.</returns>
    public T? Deserialize<T>()
    {
        if (Config is null || Config.Value.ValueKind == JsonValueKind.Undefined)
        {
            return default;
        }

        return Config.Value.Deserialize<T>(Constants.DefaultJsonSerializerOptions);
    }
}
