using System.Text.Json;

namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// Provides utilities for comparing JSON structures and reporting property-level changes.
/// </summary>
/// <remarks>
/// Used by versionable entity adapters to perform deep comparison of untyped objects
/// by serializing them to JSON and comparing the resulting structures.
/// </remarks>
internal static class AIJsonComparer
{
    /// <summary>
    /// Default maximum length for truncated string values in change output.
    /// </summary>
    public const int DefaultMaxValueLength = 100;

    /// <summary>
    /// Compares two objects by serializing them to JSON and detecting property-level changes.
    /// </summary>
    /// <param name="from">The original object.</param>
    /// <param name="to">The modified object.</param>
    /// <param name="basePath">The base path for property names in change output.</param>
    /// <param name="changes">The list to add detected changes to.</param>
    /// <param name="isSensitive">Optional callback to determine if a property path contains sensitive data.</param>
    /// <returns>True if comparison was successful; false if objects couldn't be compared.</returns>
    public static bool CompareObjects(
        object? from,
        object? to,
        string basePath,
        List<AIValueChange> changes,
        Func<string, bool>? isSensitive = null)
    {
        // Handle null cases
        if (from == null && to == null)
        {
            return true;
        }

        if (from == null)
        {
            changes.Add(new AIValueChange(basePath, null, "(configured)"));
            return true;
        }

        if (to == null)
        {
            changes.Add(new AIValueChange(basePath, "(configured)", null));
            return true;
        }

        // Serialize both to JSON for comparison
        string fromJson, toJson;
        try
        {
            fromJson = JsonSerializer.Serialize(from, from.GetType(), Constants.DefaultJsonSerializerOptions);
            toJson = JsonSerializer.Serialize(to, to.GetType(), Constants.DefaultJsonSerializerOptions);
        }
        catch
        {
            // Serialization failed
            return false;
        }

        // If JSON is identical, no changes
        if (fromJson == toJson)
        {
            return true;
        }

        // Parse JSON to find specific property changes
        try
        {
            using var fromDoc = JsonDocument.Parse(fromJson);
            using var toDoc = JsonDocument.Parse(toJson);

            CompareElements(fromDoc.RootElement, toDoc.RootElement, basePath, changes, isSensitive);
            return true;
        }
        catch
        {
            // JSON parsing failed
            return false;
        }
    }

    /// <summary>
    /// Compares two JSON elements and reports property-level changes.
    /// </summary>
    /// <param name="from">The original JSON element.</param>
    /// <param name="to">The modified JSON element.</param>
    /// <param name="path">The current property path.</param>
    /// <param name="changes">The list to add detected changes to.</param>
    /// <param name="isSensitive">Optional callback to determine if a property path contains sensitive data.</param>
    public static void CompareElements(
        JsonElement from,
        JsonElement to,
        string path,
        List<AIValueChange> changes,
        Func<string, bool>? isSensitive = null)
    {
        // Handle different value kinds
        if (from.ValueKind != to.ValueKind)
        {
            AddChange(path, from, to, changes, isSensitive);
            return;
        }

        switch (from.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(from, to, path, changes, isSensitive);
                break;

            case JsonValueKind.Array:
                CompareArrays(from, to, path, changes);
                break;

            default:
                // Primitive value comparison
                if (from.GetRawText() != to.GetRawText())
                {
                    AddChange(path, from, to, changes, isSensitive);
                }

                break;
        }
    }

    /// <summary>
    /// Compares two JSON objects and reports property changes.
    /// </summary>
    private static void CompareObjects(
        JsonElement from,
        JsonElement to,
        string path,
        List<AIValueChange> changes,
        Func<string, bool>? isSensitive)
    {
        var fromProps = from.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var toProps = to.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        // Check for added and modified properties
        foreach (var (name, toValue) in toProps)
        {
            var propPath = $"{path}.{name}";

            if (!fromProps.TryGetValue(name, out var fromValue))
            {
                // Property added
                AddChange(propPath, default, toValue, changes, isSensitive);
            }
            else
            {
                // Property exists in both - compare recursively
                CompareElements(fromValue, toValue, propPath, changes, isSensitive);
            }
        }

        // Check for removed properties
        foreach (var (name, fromValue) in fromProps)
        {
            if (!toProps.ContainsKey(name))
            {
                var propPath = $"{path}.{name}";
                AddChange(propPath, fromValue, default, changes, isSensitive);
            }
        }
    }

    /// <summary>
    /// Compares two JSON arrays.
    /// </summary>
    private static void CompareArrays(
        JsonElement from,
        JsonElement to,
        string path,
        List<AIValueChange> changes)
    {
        var fromArray = from.EnumerateArray().ToList();
        var toArray = to.EnumerateArray().ToList();

        // Simple comparison: report if arrays differ
        if (fromArray.Count != toArray.Count ||
            from.GetRawText() != to.GetRawText())
        {
            changes.Add(new AIValueChange(
                path,
                $"[{fromArray.Count} items]",
                $"[{toArray.Count} items]"));
        }
    }

    /// <summary>
    /// Adds a property change, optionally masking sensitive values.
    /// </summary>
    private static void AddChange(
        string path,
        JsonElement from,
        JsonElement to,
        List<AIValueChange> changes,
        Func<string, bool>? isSensitive)
    {
        var sensitive = isSensitive?.Invoke(path) ?? false;

        string? fromValue, toValue;

        if (sensitive)
        {
            // Mask sensitive values but indicate if they changed
            fromValue = from.ValueKind != JsonValueKind.Undefined && from.ValueKind != JsonValueKind.Null
                ? "********"
                : null;
            toValue = to.ValueKind != JsonValueKind.Undefined && to.ValueKind != JsonValueKind.Null
                ? "********"
                : null;
        }
        else
        {
            fromValue = FormatValue(from);
            toValue = FormatValue(to);
        }

        changes.Add(new AIValueChange(path, fromValue, toValue));
    }

    /// <summary>
    /// Formats a JSON element value for display in change output.
    /// </summary>
    /// <param name="element">The JSON element to format.</param>
    /// <param name="maxLength">Maximum length for string values before truncation.</param>
    /// <returns>A formatted string representation of the value.</returns>
    public static string? FormatValue(JsonElement element, int maxLength = DefaultMaxValueLength)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            JsonValueKind.String => TruncateValue(element.GetString(), maxLength),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            JsonValueKind.Object => "(object)",
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Truncates a string value for display in change logs.
    /// </summary>
    /// <param name="value">The value to truncate.</param>
    /// <param name="maxLength">Maximum length before truncation.</param>
    /// <returns>The truncated value with "..." suffix if truncated.</returns>
    public static string? TruncateValue(string? value, int maxLength = DefaultMaxValueLength)
    {
        if (value == null)
        {
            return null;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }
}
