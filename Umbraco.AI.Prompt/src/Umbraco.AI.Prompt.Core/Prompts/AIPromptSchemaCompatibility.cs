using System.Text.Json.Nodes;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Determines whether a property's resolved JSON Schema can be faithfully expressed as a
/// <em>strict</em> structured-output schema — the subset enforced by providers such as OpenAI's
/// Responses API (every node must have a resolvable <c>type</c>; arrays must declare <c>items</c>).
/// </summary>
/// <remarks>
/// <para>
/// Some CMS property editors expose a value schema that contains intentionally unconstrained nodes.
/// The block editors (Block List / Block Grid) are the canonical example: their per-property
/// <c>values[].value</c> node is an empty schema (<c>{}</c>) because the shape depends on which
/// element property editor occupies the slot (see <c>BlockJsonSchemaHelper.CreateBlockItemDataSchema</c>
/// in Umbraco CMS). There is no faithful strict-schema equivalent for "any JSON value", so embedding
/// such a schema in a structured-output request is rejected by strict providers with an
/// <c>invalid_json_schema</c> error.
/// </para>
/// <para>
/// This check is deliberately narrow. It flags only the cases a strict provider genuinely cannot
/// repair — a node with no resolvable type, or an array without <c>items</c>. It does <em>not</em>
/// require <c>additionalProperties: false</c> or that every property appears in <c>required</c>,
/// because the provider integration (M.E.AI) normalises those automatically. This keeps
/// well-typed editor schemas (e.g. ColorPicker's <c>{ value, label }</c>) representable while
/// rejecting genuinely polymorphic ones.
/// </para>
/// </remarks>
internal static class AIPromptSchemaCompatibility
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="schema"/> can be faithfully represented as a strict
    /// structured-output schema; <c>false</c> when it contains an unconstrained node (e.g. a block
    /// editor's <c>{}</c> value) that a strict provider would reject.
    /// </summary>
    public static bool IsStrictRepresentable(JsonNode? schema) => IsNodeRepresentable(schema);

    private static bool IsNodeRepresentable(JsonNode? node)
    {
        // A boolean schema (`true`/`false`) constrains nothing usable in strict mode.
        if (node is not JsonObject schema)
        {
            return false;
        }

        // A `$ref` stands in for a named definition; assume it resolves to a representable schema.
        if (schema.ContainsKey("$ref"))
        {
            return true;
        }

        // Combinators are representable when every branch is. `type` is optional alongside these.
        foreach (var combinator in new[] { "anyOf", "oneOf", "allOf" })
        {
            if (schema[combinator] is JsonArray branches)
            {
                return branches.All(IsNodeRepresentable);
            }
        }

        // `enum`/`const` pin the value to concrete literals, so a missing `type` is fine.
        if (schema.ContainsKey("enum") || schema.ContainsKey("const"))
        {
            return true;
        }

        // Every remaining node must declare a type. An empty `{}` (the block editor "any value"
        // node) has none — this is the case strict providers reject and we must detect.
        if (!TryGetTypes(schema, out var types))
        {
            return false;
        }

        if (types.Contains("object") && schema["properties"] is JsonObject properties)
        {
            if (properties.Any(property => !IsNodeRepresentable(property.Value)))
            {
                return false;
            }
        }

        if (types.Contains("array"))
        {
            // A strict array schema must declare its item shape.
            if (schema["items"] is not JsonNode items)
            {
                return false;
            }

            return IsNodeRepresentable(items);
        }

        return true;
    }

    // `type` may be a single string ("string") or an array of strings (["object", "null"]).
    private static bool TryGetTypes(JsonObject schema, out HashSet<string> types)
    {
        types = new HashSet<string>(StringComparer.Ordinal);

        switch (schema["type"])
        {
            case JsonValue value when value.TryGetValue<string>(out var single):
                types.Add(single);
                return true;
            case JsonArray array:
                foreach (var entry in array)
                {
                    if (entry is JsonValue v && v.TryGetValue<string>(out var name))
                    {
                        types.Add(name);
                    }
                }

                return types.Count > 0;
            default:
                return false;
        }
    }
}
