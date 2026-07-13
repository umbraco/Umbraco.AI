using System.Text.Json.Nodes;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Builds runtime JSON Schema response shapes for prompt execution, wrapping a target property's
/// own value schema (from <see cref="IAIPromptPropertyValueSchemaResolver"/>) so the LLM is
/// constrained to the exact structure the property editor expects, instead of the fixed
/// string-only shapes in <see cref="AIPromptResponseSchemas"/>.
/// </summary>
/// <remarks>
/// A wrapper object is necessary because many AI services require that the JSON schema have a
/// top-level 'type=object' — mirrors the rationale on <see cref="SingleValueResponse"/>.
/// </remarks>
internal static class AIPromptDynamicResponseSchemas
{
    /// <summary>
    /// Builds the wrapped response schema for a single-value prompt (OptionCount=1):
    /// <c>{ "value": &lt;valuePropertySchema&gt; }</c>.
    /// </summary>
    public static JsonObject BuildSingleValueSchema(JsonObject valuePropertySchema)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["value"] = CloneWithoutSchemaKeyword(valuePropertySchema),
            },
            ["required"] = new JsonArray("value"),
        };
    }

    /// <summary>
    /// Builds the wrapped response schema for a multi-option prompt (OptionCount>=2):
    /// <c>{ "options": [{ "label", "value": &lt;valuePropertySchema&gt;, "description" }] }</c>.
    /// </summary>
    public static JsonObject BuildMultiOptionSchema(JsonObject valuePropertySchema)
    {
        var optionSchema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["label"] = new JsonObject { ["type"] = "string" },
                ["value"] = CloneWithoutSchemaKeyword(valuePropertySchema),
                ["description"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            },
            ["required"] = new JsonArray("label", "value"),
        };

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["options"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = optionSchema,
                },
            },
            ["required"] = new JsonArray("options"),
        };
    }

    // "$schema" is only meaningful at a document root; strip it when nesting the property's own
    // schema inside a wrapper so it doesn't confuse provider-side schema validation.
    private static JsonObject CloneWithoutSchemaKeyword(JsonObject schema)
    {
        var clone = (JsonObject)schema.DeepClone();
        clone.Remove("$schema");
        return clone;
    }
}
