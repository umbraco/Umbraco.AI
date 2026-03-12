using System.Text.Json;
using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Utility for deep-merging JsonElement values.
/// Used to merge a variation's TestFeatureConfig overrides onto the test's default config.
/// </summary>
internal static class JsonElementMergeHelper
{
    /// <summary>
    /// Deep-merges an override JsonElement onto a base JsonElement.
    /// Object properties are recursively merged; arrays and primitives are replaced.
    /// </summary>
    /// <param name="baseElement">The base configuration.</param>
    /// <param name="overrideElement">The override values to merge onto the base.</param>
    /// <returns>A new JsonElement with merged values.</returns>
    public static JsonElement DeepMerge(JsonElement baseElement, JsonElement overrideElement)
    {
        var baseNode = JsonNode.Parse(baseElement.GetRawText());
        var overrideNode = JsonNode.Parse(overrideElement.GetRawText());

        var merged = MergeNodes(baseNode, overrideNode);

        var json = merged?.ToJsonString() ?? "{}";
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    private static JsonNode? MergeNodes(JsonNode? baseNode, JsonNode? overrideNode)
    {
        if (overrideNode is null)
        {
            return baseNode;
        }

        if (baseNode is JsonObject baseObj && overrideNode is JsonObject overrideObj)
        {
            var result = JsonNode.Parse(baseObj.ToJsonString())!.AsObject();

            foreach (var property in overrideObj)
            {
                if (result.ContainsKey(property.Key))
                {
                    var baseChild = result[property.Key];
                    var overrideChild = property.Value;

                    if (baseChild is JsonObject && overrideChild is JsonObject)
                    {
                        result[property.Key] = MergeNodes(baseChild, overrideChild);
                    }
                    else
                    {
                        // Arrays and primitives are replaced
                        result[property.Key] = property.Value is not null
                            ? JsonNode.Parse(property.Value.ToJsonString())
                            : null;
                    }
                }
                else
                {
                    result[property.Key] = property.Value is not null
                        ? JsonNode.Parse(property.Value.ToJsonString())
                        : null;
                }
            }

            return result;
        }

        // Non-object types: override replaces base
        return overrideNode is not null
            ? JsonNode.Parse(overrideNode.ToJsonString())
            : baseNode;
    }
}
